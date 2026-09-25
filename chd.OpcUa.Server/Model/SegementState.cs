using chd.OpcUa.Server.UnderlyingSystem;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server.Interfaces;

namespace chd.OpcUa.Server.Model
{
    public class SegmentState : FolderState
    {
        public UnderlyingSystemSegment Segment { get; }

        public SegmentState(NodeId nodeId, UnderlyingSystemSegment segment) : base(null)
        {
            Segment = segment;
            this.TypeDefinitionId = ObjectTypeIds.FolderType;
            this.SymbolicName = segment.Name;
            this.NodeId = nodeId;
            this.BrowseName = new QualifiedName(segment.Name, nodeId.NamespaceIndex);
            this.DisplayName = new LocalizedText(segment.Name);
            this.Description = new LocalizedText(segment.Description);
            this.WriteMask = 0;
            this.UserWriteMask = 0;
            this.EventNotifier = EventNotifiers.None;
        }


        public override INodeBrowser CreateBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly)
        {
            var browser = new SegmentBrowser(context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly,
                this);

            PopulateBrowser(context, browser);

            return browser;
        }

        protected override void PopulateBrowser(ISystemContext context, NodeBrowser browser)
        {
            base.PopulateBrowser(context, browser);

            if (browser.IsRequired(ReferenceTypeIds.Organizes, true))
            {
                if (Segment.Parent is not null)
                {
                    browser.Add(ReferenceTypeIds.Organizes, true, ModelUtils.ConstructIdForSegment(Segment.Parent.Identifier, this.NodeId.NamespaceIndex));
                }
            }
        }
    }
}
