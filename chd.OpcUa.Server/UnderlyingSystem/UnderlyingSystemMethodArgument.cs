using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemMethodArgument
    {
        private readonly string _identifier;
        public UnderlyingSystemMethod Method { get; }
        public string Name { get; set; }
        public Type Type { get; set; }

        public string Description { get; set; }


        public UnderlyingSystemMethodArgument(UnderlyingSystemMethod method, string name, Type type)
        {
            Method = method;
            Name = name;
            Type = type;
        }

        public NodeId GetDataType() => Type switch
        {
            Type x when x == typeof(bool) => DataTypeIds.Boolean,
            Type x when x == typeof(sbyte) => DataTypeIds.SByte,
            Type x when x == typeof(byte) => DataTypeIds.Byte,
            Type x when x == typeof(short) => DataTypeIds.Int16,
            Type x when x == typeof(ushort) => DataTypeIds.UInt16,
            Type x when x == typeof(int) => DataTypeIds.Int32,
            Type x when x == typeof(uint) => DataTypeIds.UInt32,
            Type x when x == typeof(long) => DataTypeIds.Int64,
            Type x when x == typeof(float) => DataTypeIds.Float,
            Type x when x == typeof(double) => DataTypeIds.Double,
            Type x when x == typeof(decimal) => DataTypeIds.Decimal,
            Type x when x == typeof(DateTime) => DataTypeIds.DateTime,
            Type x when x == typeof(string) => DataTypeIds.String,
            Type x when x == typeof(Guid) => DataTypeIds.Guid,
            _ => DataTypeIds.BaseDataType
        };

        public int GetValueRank()
        {
            return ValueRanks.Scalar;
        }
    }
}
