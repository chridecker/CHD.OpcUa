using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace chd.OpcUa.Client
{
    public class NodeDto
    {
        public ReferenceDescription Description { get; set; }
        public ExpandedNodeId Node => Description.NodeId;
        public NodeId ParentNodeId { get; set; }

        public string Identifier => Description.NodeId.IdType switch
        {
            IdType.String => Node.IdentifierAsString,
            _ => Description.BrowseName.ToString(),
        };

        public NodeDto(ReferenceDescription description, NodeId parent)
        {
            Description = description;
            ParentNodeId = parent;
        }
    }
}
