using chd.OpcUa.Server.UnderlyingSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using chd.OpcUa.Contracts.Interfaces;
using chd.OpcUa.Server.Extensions;
using Opc.Ua;
using Opc.Ua.Server;

namespace chd.OpcUa.Server
{
    public abstract class UnderlyingSystemManager : IUnderlyingSystemManager<UnderlyingSystemSegment, UnderlyingSystemBlock, UnderlyingSystemMethod>
    {
        private List<UnderlyingSystemSegment> _segments;
        private ConcurrentDictionary<string, UnderlyingSystemBlock> _blocks = [];

        public async ValueTask<List<UnderlyingSystemSegment>> GetMainSegmentsAsync(CancellationToken cancellationToken)
        {
            if (_segments is null)
            {
                _segments = await LoadSegments(cancellationToken);
            }

            return _segments;
        }

        public List<UnderlyingSystemSegment> FindSegmentsForBlock(string blockIdentifier)
         => _segments.Flatten().Where(x => x.Blocks.Contains(blockIdentifier)).ToList();

        public ValueTask<UnderlyingSystemSegment> FindSegmentByIdentifier(string identifier, CancellationToken cancellationToken)
            => ValueTask.FromResult(_segments.Flatten().FirstOrDefault(x => x.Identifier == identifier));

        public List<UnderlyingSystemBlock> FindBlocksForSegment(UnderlyingSystemSegment segment)
        {
            var lst = new List<UnderlyingSystemBlock>();
            foreach (var blockName in segment.Blocks)
            {
                if (_blocks.TryGetValue(blockName, out var block))
                    lst.Add(block);
            }

            return lst;
        }

        public ValueTask<UnderlyingSystemBlock> FindBlockByIdentifier(string identifier,
            CancellationToken cancellationToken)
        {
            if (_blocks.TryGetValue(identifier, out var block))
            {
                return ValueTask.FromResult(block);
            }

            return ValueTask.FromResult(block);
        }

        public ValueTask<UnderlyingSystemMethod> FindMethodByIdentifier(string identifier,
            CancellationToken cancellationToken)
        {
            var args = identifier.Split("#");
            if (_blocks.TryGetValue(args[0], out var block))
            {
                return ValueTask.FromResult(block.GetMethods().FirstOrDefault(x => string.Equals(x.Identifier, identifier)));
            }

            return ValueTask.FromResult((UnderlyingSystemMethod)null);
        }

        public ValueTask InitializeAsync(CancellationToken cancellationToken) => CreateBlocksAsync(cancellationToken);

        protected abstract ValueTask<List<UnderlyingSystemSegment>> LoadSegments(CancellationToken cancellationToken);

        protected abstract ValueTask<UnderlyingSystemBlock> CreateBlockAsync(string blockName, CancellationToken cancellationToken);

        protected virtual (MethodInfo? method, object instance) GetMethodInfo(UnderlyingSystemMethod method) => (this.GetType().GetMethod(method.Name), this);

        private async ValueTask CreateBlocksAsync(CancellationToken cancellationToken)
        {
            foreach (var blockName in _segments.Flatten().SelectMany(s => s.Blocks).Distinct())
            {
                var block = await CreateBlockAsync(blockName, cancellationToken);
                if (block is null) { continue; }
                block.MethodExecution += Block_MethodExecution;
                _blocks[blockName] = block;
            }
        }

        private async ValueTask<object[]> Block_MethodExecution(UnderlyingSystemMethod method, object[] inputs, CancellationToken cancellationToken)
        {
            var (execution, instance) = GetMethodInfo(method);
            if (execution is null)
            {
                throw new NotImplementedException($"Konnte die Methode {method.Name} nicht finden!");
            }

            var inputParams = execution.GetParameters();
            object[] inputsArray = inputs;
            if (!(inputParams.Length == inputsArray.Length || inputParams.Length == inputsArray.Length + 1))
            {
                throw new ArgumentOutOfRangeException("Die Inputargumente passen nicht");
            }

            if (inputParams.Length == inputsArray.Length + 1)
            {
                inputsArray = inputs.Append(cancellationToken).ToArray();
            }

            if (execution.ReturnType == typeof(ValueTask<object[]>))
            {
                return await (ValueTask<object[]>)execution.Invoke(instance, inputsArray);
            }
            if (execution.ReturnType == typeof(Task<object[]>))
            {
                return await (Task<object[]>)execution.Invoke(instance, inputsArray);
            }
            if (execution.ReturnType.IsAssignableTo(typeof(ValueTask)))
            {
                await (ValueTask)execution.Invoke(instance, inputsArray);
            }
            if (execution.ReturnType.IsAssignableTo(typeof(Task))
                && !execution.ReturnType.IsGenericType)
            {
                await (Task)execution.Invoke(instance, inputsArray);
            }
            if (execution.ReturnType == typeof(object[]))
            {
                return (object[])execution.Invoke(instance, inputsArray);
            }

            if (execution.ReturnType.IsGenericType
                && execution.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                && execution.Invoke(instance, inputsArray) is Task t)
            {
                await t;
                var result = t.GetType()
                    .GetProperty("Result")
                    ?.GetValue(t);
                return [result];
            }

            if (execution.ReturnType.IsGenericType
                && execution.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var valueTask = execution.Invoke(instance, inputsArray);
                var task = (Task)valueTask.GetType()
                    .GetMethod("AsTask")!
                    .Invoke(valueTask, null)!;
                await task;
                var result = task.GetType()
                    .GetProperty("Result")
                    ?.GetValue(task);
                return [result];
            }
            if (execution.ReturnType == typeof(void))
            {
                execution.Invoke(instance, inputsArray);
            }
            if (execution.ReturnType != typeof(void)
                && execution.ReturnType.IsValueType && !execution.ReturnType.IsGenericType)
            {
                return [execution.Invoke(instance, inputsArray)];
            }

            return [];
        }
    }
}
