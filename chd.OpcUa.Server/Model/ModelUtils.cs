using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.Model
{
    public static class ModelUtils
    {
        public const int Segment = 0;

        public const int Block = 1;

        public static NodeId ConstructIdForSegment(string identifier, ushort namespaceIndex)
        {
            var parsedNodeId = new ParsedNodeId
            {
                RootId = identifier,
                NamespaceIndex = namespaceIndex,
                RootType = Segment,
            };
            return parsedNodeId.Construct();
        }

        public static NodeId ConstructIdForBlock(string blockId, ushort namespaceIndex)
        {
            var parsedNodeId = new ParsedNodeId
            {
                RootId = blockId,
                NamespaceIndex = namespaceIndex,
                RootType = Block
            };
            return parsedNodeId.Construct();
        }

        public static NodeId ConstructIdForComponent(NodeState component, ushort namespaceIndex)
        {
            if (component is null)
            {
                return NodeId.Null;
            }

            var instance = component as BaseInstanceState;

            if (instance?.Parent is null)
            {
                return component.NodeId;
            }

            var parentId = instance.Parent.NodeId.TryGetValue(out string id) ? id : null;

            if (parentId is null)
            {
                return NodeId.Null;
            }

            var buffer = new StringBuilder();
            buffer.Append(parentId);

            var index = parentId.IndexOf('?', StringComparison.Ordinal);

            if (index < 0)
            {
                buffer.Append('?');
            }
            else
            {
                buffer.Append('/');
            }

            buffer.Append(component.SymbolicName);

            return new NodeId(buffer.ToString(), namespaceIndex);
        }
    }
}
