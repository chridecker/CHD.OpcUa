using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemBlock
    {
        private readonly ConcurrentBag<UnderlyingSystemTag> _tags = [];
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

        public void CreateTag(string tagName, UnderlyingSystemDataType dataType, UnderlyingSystemTagType tagType, string engineeringUnits, bool writeable)
        {
            var tag = new UnderlyingSystemTag
            {
                Block = this,
                Name = tagName,
                EngineeringUnits = engineeringUnits,
                DataType = dataType,
                TagType = tagType,
                IsWriteable = writeable,
                Labels = null,
                EuRange = null
            };

            switch (tagType)
            {
                case UnderlyingSystemTagType.Analog:
                    {
                        tag.Description = "An analog value.";
                        tag.TagType = UnderlyingSystemTagType.Analog;
                        tag.EuRange = new double[] { 100, 0 };
                        break;
                    }

                case UnderlyingSystemTagType.Digital:
                    {
                        tag.Description = "A digital value.";
                        tag.TagType = UnderlyingSystemTagType.Digital;
                        tag.Labels = new string[] { "Online", "Offline" };
                        break;
                    }

                case UnderlyingSystemTagType.Enumerated:
                    {
                        tag.Description = "An enumerated value.";
                        tag.TagType = UnderlyingSystemTagType.Enumerated;
                        tag.Labels = new string[] { "Red", "Yellow", "Green" };
                        break;
                    }

                default:
                    {
                        tag.Description = "A generic value.";
                        break;
                    }
            }

            switch (tag.DataType)
            {
                case UnderlyingSystemDataType.Integer1: { tag.Value = (sbyte)0; break; }
                case UnderlyingSystemDataType.Integer2: { tag.Value = (short)0; break; }
                case UnderlyingSystemDataType.Integer4: { tag.Value = (int)0; break; }
                case UnderlyingSystemDataType.Real4: { tag.Value = (float)0; break; }
                case UnderlyingSystemDataType.String: { tag.Value = string.Empty; break; }
            }
            _tags.Add(tag);
            Timestamp = DateTime.UtcNow;
        }

        public IList<UnderlyingSystemTag> GetTags() => _tags.Select(s => s.CreateSnapshot()).ToList();

        public StatusCode WriteTagValue(string tagName, Variant value)
        {
            var tag = _tags.FirstOrDefault(x => x.Name == tagName);

            if (tag is null)
            {
                return StatusCodes.BadNodeIdUnknown;
            }

            switch (tag.DataType)
            {
                case UnderlyingSystemDataType.Integer1:
                    {
                        if (!value.TryGetValue(out sbyte int1Value))
                        {
                            return StatusCodes.BadTypeMismatch;
                        }

                        tag.Value = int1Value;
                        break;
                    }

                case UnderlyingSystemDataType.Integer2:
                    {
                        if (!value.TryGetValue(out short int2Value))
                        {
                            return StatusCodes.BadTypeMismatch;
                        }

                        tag.Value = int2Value;
                        break;
                    }

                case UnderlyingSystemDataType.Integer4:
                    {
                        if (!value.TryGetValue(out int int4Value))
                        {
                            return StatusCodes.BadTypeMismatch;
                        }

                        tag.Value = int4Value;
                        break;
                    }

                case UnderlyingSystemDataType.Real4:
                    {
                        if (!value.TryGetValue(out float real4Value))
                        {
                            return StatusCodes.BadTypeMismatch;
                        }

                        tag.Value = real4Value;
                        break;
                    }

                case UnderlyingSystemDataType.String:
                    {
                        if (!value.TryGetValue(out string stringValue))
                        {
                            return StatusCodes.BadTypeMismatch;
                        }

                        tag.Value = stringValue;
                        break;
                    }
            }

            tag.Timestamp = DateTime.UtcNow;

            // raise notification.
            if (tag is not null)
            {
                OnTagsChanged?.Invoke(this, tag);
            }

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
    }
}
