using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace CHD.OPCUA.Client.Extensions
{
    public static class DataValueExtensions
    {

        public static object GetValue(this DataValue value)
        {
            switch (value.WrappedValue.TypeInfo.BuiltInType)
            {
                case BuiltInType.Boolean:
                    {
                        if (value.WrappedValue.TryGetValue(out bool val)) ;
                        {
                            return val;
                        }
                        return false;
                    }
                case BuiltInType.SByte:

                    {
                        if (value.WrappedValue.TryGetValue(out sbyte val))
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.Byte:
                    {
                        if (value.WrappedValue.TryGetValue(out byte val)) ;
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.Int16:
                    {
                        if (value.WrappedValue.TryGetValue(out short val))
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.UInt16:
                    {
                        if (value.WrappedValue.TryGetValue(out ushort val))
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.Int32:
                    {
                        if (value.WrappedValue.TryGetValue(out int val)) ;
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.UInt32:
                    {
                        if (value.WrappedValue.TryGetValue(out uint val)) ;
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.Int64:
                    {
                        if (value.WrappedValue.TryGetValue(out long val)) ;
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.UInt64:
                    {
                        if (value.WrappedValue.TryGetValue(out ulong val))
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.Float:
                    {
                        if (value.WrappedValue.TryGetValue(out float val))
                        {
                            return val;
                        }
                        return 0;
                    }

                case BuiltInType.Double:
                    {
                        if (value.WrappedValue.TryGetValue(out double val))
                        {
                            return val;
                        }

                        return 0;
                    }

                default:
                    {
                        return value.WrappedValue.Value;
                    }
            }
        }

        public static Variant ChangeType(this DataValue value, object newValue)
        {
            switch (value.WrappedValue.TypeInfo.BuiltInType)
            {
                case BuiltInType.Boolean:
                    {
                        return Variant.From(Convert.ToBoolean(newValue));
                    }

                case BuiltInType.SByte:
                    {
                        return Variant.From(Convert.ToSByte(newValue));
                    }

                case BuiltInType.Byte:
                    {
                        return Variant.From(Convert.ToByte(newValue));
                    }

                case BuiltInType.Int16:
                    {
                        return Variant.From(Convert.ToInt16(newValue));
                    }

                case BuiltInType.UInt16:
                    {
                        return Variant.From(Convert.ToUInt16(newValue));
                    }

                case BuiltInType.Int32:
                    {
                        return Variant.From(Convert.ToInt32(newValue));
                    }

                case BuiltInType.UInt32:
                    {
                        return Variant.From(Convert.ToUInt32(newValue));
                    }

                case BuiltInType.Int64:
                    {
                        return Variant.From(Convert.ToInt64(newValue));
                    }

                case BuiltInType.UInt64:
                    {
                        return Variant.From(Convert.ToUInt64(newValue));
                    }

                case BuiltInType.Float:
                    {
                        return Variant.From(Convert.ToSingle(newValue));
                    }

                case BuiltInType.Double:
                    {
                        return Variant.From(Convert.ToDouble(newValue));
                    }

                default:
                    {
                        return Variant.From(newValue.ToString());
                    }
            }
        }
    }
}
