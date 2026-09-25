using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemTag : UnderlyingSystemBase
    {
        public UnderlyingSystemBlock Block { get; set; }

        public Type Type { get; set; }

        public Variant Value { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsWriteable { get; }

        public string[] Labels { get; set; }

        public Func<CancellationToken, ValueTask<Variant>> ReadFunc { get; set; }
        public Func<Variant, CancellationToken, ValueTask> WriteFunc { get; set; }

        public UnderlyingSystemTag(string name, bool isWriteable) : base(name)
        {
            IsWriteable = isWriteable;
        }

        public UnderlyingSystemTag CreateSnapshot() => (UnderlyingSystemTag)MemberwiseClone();
    }
}
