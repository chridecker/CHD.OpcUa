using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server.UnderlyingSystem;

namespace chd.OpcUa.Server.Extensions
{
    public static class UnderlyingSystemExtensions
    {
        public static List<UnderlyingSystemSegment> Flatten(this List<UnderlyingSystemSegment> segments)
        {
            var result = new List<UnderlyingSystemSegment>();
            foreach (var root in segments)
            {
                AddRecursive(root, result);
            }

            return result;
        }

        private static void AddRecursive(
            UnderlyingSystemSegment segment,
            List<UnderlyingSystemSegment> result)
        {
            result.Add(segment);

            foreach (var child in segment.Children)
            {
                AddRecursive(child, result);
            }
        }
    }
}
