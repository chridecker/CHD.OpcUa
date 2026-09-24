using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Base.Extensions;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemBlock
    {
        private readonly ConcurrentBag<UnderlyingSystemTag> _tags = [];
        private readonly ConcurrentBag<UnderlyingSystemMethod> _methods = [];
        private event EventHandler<UnderlyingSystemTag> OnTagsChanged;

        public string Name { get; set; }
        public string BlockType { get; set; }
        public string NameSpace { get; set; }
        public string Identifier => Name;
        public DateTime Timestamp { get; set; }

        public UnderlyingSystemBlock(string name, string blockType)
        {
            Name = name;
            BlockType = blockType;
        }

        public void CreateMethod(string name, Action<UnderlyingSystemMethod> handleMethod)
        {
            var method = new UnderlyingSystemMethod(name, this, this.MethodExecuted);
            handleMethod(method);
            _methods.Add(method);
        }

        public void CreateTag<T>(string tagName, string description, bool writeable, string[] labels = null)
        {
            var tag = new UnderlyingSystemTag
            {
                Block = this,
                Name = tagName,
                Description = description,
                Type = typeof(T),
                IsWriteable = writeable,
                Labels = labels,
            };
            tag.Value = tag.Type.IsEnum ? 0 : tag.Type.IsValueType ? new Variant(Activator.CreateInstance(tag.Type)) : null;
            _tags.Add(tag);
            Timestamp = DateTime.UtcNow;
        }

        public IList<UnderlyingSystemTag> GetTags() => _tags.Select(s => s.CreateSnapshot()).ToList();

        public IList<UnderlyingSystemMethod> GetMethods() => _methods.Select(s => s.CreateSnapshot()).ToList();

        public StatusCode WriteTagValue(string tagName, Variant value)
        {
            var tag = _tags.FirstOrDefault(x => x.Name == tagName);

            if (tag is null)
            {
                return StatusCodes.BadNodeIdUnknown;
            }

            var val = value.GetValue();
            if (val.GetType() != tag.Type)
            {
                return StatusCodes.BadTypeMismatch;
            }

            tag.Value = new Variant(val);

            tag.Timestamp = DateTime.UtcNow;

            OnTagsChanged?.Invoke(this, tag);

            return StatusCodes.Good;
        }
        public void StartMonitoring(EventHandler<UnderlyingSystemTag> callback)
        {
            OnTagsChanged = callback;
        }
        public void StopMonitoring()
        {
            OnTagsChanged = null;
        }

        public event UnderlyingSystemMethodExcutionHandler MethodExecution;


        private ValueTask<object[]> MethodExecuted(UnderlyingSystemMethod method, object[] inputs, CancellationToken cancellationToken)
        {
            if (MethodExecution is not null)
            {
                return MethodExecution.Invoke(method, inputs, cancellationToken);
            }

            return ValueTask.FromResult(Array.Empty<object>());
        }
    }

    public delegate ValueTask<object[]> UnderlyingSystemMethodExcutionHandler(UnderlyingSystemMethod method,
        object[] inputs, CancellationToken cancellationToken);
}
