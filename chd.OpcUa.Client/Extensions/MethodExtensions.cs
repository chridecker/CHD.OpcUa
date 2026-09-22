using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace chd.OpcUa.Client.Extensions
{
    public static class MethodExtensions
    {
        public static object[] ToInputArray<T>(this T input) where T : struct
        {
            return typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.MetadataToken)
                .Select(p => p.GetValue(input))
                .ToArray();
        }

        public static T ToOuputData<T>(this IEnumerable<object> output) where T : struct
            => (T)Activator.CreateInstance(typeof(T), output.ToArray());
    }
}
