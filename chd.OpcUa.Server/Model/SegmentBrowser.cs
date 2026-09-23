using chd.OpcUa.Server.UnderlyingSystem;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server.Interfaces;

namespace chd.OpcUa.Server.Model
{
    public class SegmentBrowser : NodeBrowser
    {
        private Stage _stage;
        private int _position;
        private SegmentState _source;
        private IList<UnderlyingSystemSegment> _segments;

        public SegmentBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly,
            SegmentState source)
        :
            base(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly)
        {
            _source = source;
            _stage = Stage.Begin;
        }

        public override IReference Next()
        {
            var reference = base.Next();

            if (reference is not null)
            {
                return reference;
            }

            if (_stage is Stage.Begin)
            {
                _segments = _source.Segment.Children.ToList();
                _stage = Stage.Segments;
                _position = 0;
            }

            if (InternalOnly)
            {
                return null;
            }

            if (_stage is Stage.Segments)
            {
                if (IsRequired(ReferenceTypeIds.Organizes, false))
                {
                    reference = NextChild();

                    if (reference is not null)
                    {
                        return reference;
                    }
                }
                _stage = Stage.Blocks;
                _position = 0;
            }

            if (_stage is Stage.Blocks)
            {
                if (IsRequired(ReferenceTypeIds.Organizes, false))
                {
                    reference = NextChild();

                    if (reference is not null)
                    {
                        return reference;
                    }

                    _stage = Stage.Done;
                    _position = 0;
                }
            }

            return null;
        }

        private IReference NextChild()
        {
            var targetId = NodeId.Null;

            if (!(base.BrowseName).IsNull)
            {
                if (_position == int.MaxValue)
                {
                    return null;
                }

                if (_source.BrowseName.NamespaceIndex != base.BrowseName.NamespaceIndex)
                {
                    return null;
                }

                if (_stage is Stage.Segments && _segments is not null)
                {
                    for (int ii = 0; ii < _segments.Count; ii++)
                    {
                        if (base.BrowseName.Name == _segments[ii].Name)
                        {
                            targetId = ModelUtils.ConstructIdForSegment(_segments[ii].Identifier, _source.NodeId.NamespaceIndex);
                            break;
                        }
                    }
                }

                if (_stage is Stage.Blocks && _source.Segment.Blocks is not null)
                {
                    foreach (var block in _source.Segment.Blocks)
                    {
                        if (block is not null && base.BrowseName.Name == block)
                        {
                            targetId = ModelUtils.ConstructIdForBlock(block, _source.NodeId.NamespaceIndex);
                            break;
                        }
                    }
                }

                _position = int.MaxValue;
            }
            else
            {
                if (_stage is Stage.Segments && _segments is not null)
                {
                    if (_position >= _segments.Count)
                    {
                        return null;
                    }

                    targetId = ModelUtils.ConstructIdForSegment(_segments[_position++].Identifier, _source.NodeId.NamespaceIndex);
                }

                else if (_stage is Stage.Blocks && _source.Segment.Blocks is not null)
                {
                    if (_position >= _source.Segment.Blocks.Count)
                    {
                        return null;
                    }

                    targetId = ModelUtils.ConstructIdForBlock(_source.Segment.Blocks.ElementAt(_position++), _source.NodeId.NamespaceIndex);
                }
            }

            if (!targetId.IsNull)
            {
                return new NodeStateReference(ReferenceTypeIds.Organizes, false, targetId);
            }

            return null;
        }

        enum Stage { Begin, Segments, Blocks, Done }
    }
}
