using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.Server.UnderlyingSystem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server.Extensions;
using Opc.Ua;
using Opc.Ua.Server;

namespace chd.OpcUa.Server
{
    public abstract class UnderlyingSystemManager : IUnderlyingSystemManager
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

        public ValueTask InitializeAsync(CancellationToken cancellationToken)
        => CreateBlocksAsync(cancellationToken);


        protected abstract ValueTask<List<UnderlyingSystemSegment>> LoadSegments(CancellationToken cancellationToken);
        protected abstract ValueTask<UnderlyingSystemBlock> CreateBlockAsync(string blockName, CancellationToken cancellationToken);

        private async ValueTask CreateBlocksAsync(CancellationToken cancellationToken)
        {
            foreach (var blockName in _segments.Flatten().SelectMany(s => s.Blocks).Distinct())
            {
                var block = await CreateBlockAsync(blockName, cancellationToken);
                block.MethodExecution += Block_MethodExecution;
                _blocks[blockName] = block;
            }
        }
        private async ValueTask<object[]> Block_MethodExecution(UnderlyingSystemMethod method, object[] inputs, CancellationToken cancellationToken)
        {
            var execution = this.GetType().GetMethod(method.Name);
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

            var result = execution.Invoke(this, inputsArray);
            if (execution.ReturnType == typeof(ValueTask<object[]>))
            {
                return await (ValueTask<object[]>)result;
            }
            if (execution.ReturnType == typeof(Task<object[]>))
            {
                return await (Task<object[]>)result;
            }
            if (execution.ReturnType.IsAssignableTo(typeof(ValueTask)))
            {
                await (ValueTask)result;
            }
            if (execution.ReturnType.IsAssignableTo(typeof(Task)))
            {
                await (Task)result;
            }
            if (execution.ReturnType == typeof(object[]))
            {
                return (object[])result;
            }

            if ((execution.ReturnType.IsAssignableTo(typeof(ValueTask))
                 || execution.ReturnType.IsAssignableTo(typeof(Task)))
                && execution.ReturnType.IsGenericType)
            {
                result = result.GetType()
                    .GetProperty("Result")
                    ?.GetValue(result);
            }
            if (execution.ReturnType == typeof(void))
            {
                return [];
            }
            return [result];
        }
    }
}
