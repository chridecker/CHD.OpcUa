using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemTag
    {
        public UnderlyingSystemBlock Block { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string EngineeringUnits { get; set; }

        public UnderlyingSystemDataType DataType { get; set; }

        public UnderlyingSystemTagType TagType { get; set; }

        public Variant Value { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsWriteable { get; set; }

        public double[] EuRange { get; set; }
        public string[] Labels { get; set; }

        public UnderlyingSystemTag CreateSnapshot() => (UnderlyingSystemTag)MemberwiseClone();
    }
}
