using chd.OpcUa.Server.UnderlyingSystem;
using chd.OpcUa.ServerWorker;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.Model
{
    public class BlockState : BaseObjectState
    {
        private UnderlyingSystemBlock _block;
        private NodeManager _nodeManager;
        private int _monitoringCount;

        public BlockState(NodeManager nodeManager, NodeId nodeId, UnderlyingSystemBlock block) : base(null)
        {
            _nodeManager = nodeManager;
            _block = block;

            this.TypeDefinitionId = ObjectTypeIds.BaseObjectType;
            this.SymbolicName = block.Name;
            this.NodeId = nodeId;
            this.BrowseName = new QualifiedName(block.Name, nodeId.NamespaceIndex);
            this.DisplayName = new LocalizedText(block.Name);
            this.Description = LocalizedText.Null;
            this.WriteMask = 0;
            this.UserWriteMask = 0;
            this.EventNotifier = EventNotifiers.None;

            foreach (var tag in block.GetTags())
            {
                var variable = CreateVariable(nodeManager.SystemContext, tag);
                AddChild(variable);
                variable.OnSimpleWriteValueAsync = OnWriteTagValueAsync;
            }
        }

        public ValueTask<AttributeWriteResult> OnWriteTagValueAsync(
            ISystemContext context,
            NodeState node,
            Variant value,
            CancellationToken cancellationToken)
        {
            if (_block is null)
            {
                return new ValueTask<AttributeWriteResult>(
                    new AttributeWriteResult(StatusCodes.BadNodeIdUnknown));
            }

            var error = _block.WriteTagValue(node.SymbolicName, value);

            if (error != 0)
            {
                return new ValueTask<AttributeWriteResult>(new AttributeWriteResult(error));
            }

            return new ValueTask<AttributeWriteResult>(new AttributeWriteResult(ServiceResult.Good));
        }

        public void StartMonitoring(ServerSystemContext context)
        {
            if (_monitoringCount == 0)
            {
                _block?.StartMonitoring(OnTagsChanged);
            }
            _monitoringCount++;
        }

        /// <summary>
        /// Stop the monitoring the block.
        /// </summary>
        /// <param name="context">The context.</param>
        public bool StopMonitoring(ServerSystemContext context)
        {
            _monitoringCount--;

            if (_monitoringCount == 0)
            {
                _block?.StopMonitoring();
            }

            return _monitoringCount != 0;
        }

        private void OnTagsChanged(object sender, UnderlyingSystemTag tag)
        {
            var variable = FindChildBySymbolicName(_nodeManager.SystemContext, tag.Name) as BaseVariableState;
            if (variable is not null)
            {
                UpdateVariable(_nodeManager.SystemContext, tag, variable);
            }
            this.ClearChangeMasks(_nodeManager.SystemContext, true);
        }

        protected override void PopulateBrowser(ISystemContext context, NodeBrowser browser)
        {
            base.PopulateBrowser(context, browser);
            if (browser.BrowseDirection != BrowseDirection.Inverse)
            {
                var tags = new List<BaseInstanceState>();
                GetChildren(context, tags);

                for (int ii = 0; ii < tags.Count; ii++)
                {
                    browser.Add(tags[ii].ReferenceTypeId, false, tags[ii]);
                }
            }

            // check if the parent segments need to be returned.
            if (browser.IsRequired(ReferenceTypeIds.Organizes, true))
            {
                foreach (var segment in _nodeManager.UnderlyingSystemManager.FindSegmentsForBlock(_block.Identifier))
                {
                    browser.Add(ReferenceTypeIds.Organizes, true, ModelUtils.ConstructIdForSegment(segment.Identifier, this.NodeId.NamespaceIndex));
                }
            }
        }


        private BaseVariableState CreateVariable(ISystemContext context, UnderlyingSystemTag tag)
        {
            // create the variable type based on the tag type.
            BaseDataVariableState variable = null;

            switch (tag.TagType)
            {
                case UnderlyingSystemTagType.Analog:
                    {
                        AnalogItemState node = new AnalogItemState(this);

                        if (tag.EngineeringUnits != null)
                        {
                            node.EngineeringUnits = PropertyState<EUInformation>.With<StructureBuilder<EUInformation>>(node);
                        }

                        if (tag.EuRange.Length >= 4)
                        {
                            node.InstrumentRange = PropertyState<Opc.Ua.Range>.With<StructureBuilder<Opc.Ua.Range>>(node);
                        }

                        variable = node;
                        break;
                    }

                case UnderlyingSystemTagType.Digital:
                    {
                        TwoStateDiscreteState node = new TwoStateDiscreteState(this);
                        variable = node;
                        break;
                    }

                case UnderlyingSystemTagType.Enumerated:
                    {
                        MultiStateDiscreteState node = new MultiStateDiscreteState(this);

                        if (tag.Labels != null)
                        {
                            node.EnumStrings = PropertyState<ArrayOf<LocalizedText>>.With<VariantBuilder>(node);
                        }

                        variable = node;
                        break;
                    }

                default:
                    {
                        DataItemState node = new DataItemState(this);
                        variable = node;
                        break;
                    }
            }

            // set the symbolic name and reference types.
            variable.SymbolicName = tag.Name;
            variable.ReferenceTypeId = ReferenceTypeIds.HasComponent;

            // initialize the variable from the type model.
            variable.Create(
                context,
                NodeId.Null,
                new QualifiedName(tag.Name, this.BrowseName.NamespaceIndex),
                LocalizedText.Null,
                true);

            // update the variable values.
            UpdateVariable(context, tag, variable);
            return variable;
        }
        private void UpdateVariable(ISystemContext context, UnderlyingSystemTag tag, BaseVariableState variable)
        {
            variable.Description = new LocalizedText(tag.Description);
            variable.Value = tag.Value;
            variable.Timestamp = tag.Timestamp;

            switch (tag.DataType)
            {
                case UnderlyingSystemDataType.Integer1: { variable.DataType = new NodeId(DataTypes.SByte); break; }
                case UnderlyingSystemDataType.Integer2: { variable.DataType = new NodeId(DataTypes.Int16); break; }
                case UnderlyingSystemDataType.Integer4: { variable.DataType = new NodeId(DataTypes.Int32); break; }
                case UnderlyingSystemDataType.Real4: { variable.DataType = new NodeId(DataTypes.Float); break; }
                case UnderlyingSystemDataType.String: { variable.DataType = new NodeId(DataTypes.String); break; }
            }

            variable.ValueRank = ValueRanks.Scalar;
            variable.ArrayDimensions = ArrayOf<uint>.Empty;

            if (tag.IsWriteable)
            {
                variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            }
            else
            {
                variable.AccessLevel = AccessLevels.CurrentRead;
                variable.UserAccessLevel = AccessLevels.CurrentRead;
            }

            variable.MinimumSamplingInterval = MinimumSamplingIntervals.Continuous;
            variable.Historizing = false;

            switch (tag.TagType)
            {
                case UnderlyingSystemTagType.Analog:
                    {
                        AnalogItemState node = variable as AnalogItemState;

                        if (tag.EuRange is not null)
                        {
                            if (tag.EuRange.Length >= 2 && node.EURange != null)
                            {
                                Opc.Ua.Range range = new Opc.Ua.Range(tag.EuRange[0], tag.EuRange[1]);
                                node.EURange.Value = range;
                                node.EURange.Timestamp = tag.Block.Timestamp;
                            }

                            if (tag.EuRange.Length >= 4 && node.InstrumentRange != null)
                            {
                                Opc.Ua.Range range = new Opc.Ua.Range(tag.EuRange[2], tag.EuRange[3]);
                                node.InstrumentRange.Value = range;
                                node.InstrumentRange.Timestamp = tag.Block.Timestamp;
                            }
                        }

                        if (!string.IsNullOrEmpty(tag.EngineeringUnits) && node.EngineeringUnits != null)
                        {
                            var info = new EUInformation
                            {
                                DisplayName = new LocalizedText(tag.EngineeringUnits),
                                NamespaceUri = _block.NameSpace
                            };
                            node.EngineeringUnits.Value = info;
                            node.EngineeringUnits.Timestamp = tag.Block.Timestamp;
                        }

                        break;
                    }

                case UnderlyingSystemTagType.Digital:
                    {
                        TwoStateDiscreteState node = variable as TwoStateDiscreteState;

                        if (tag.Labels is not null && node.TrueState != null && node.FalseState != null)
                        {
                            if (tag.Labels.Length >= 2)
                            {
                                node.TrueState.Value = new LocalizedText(tag.Labels[0]);
                                node.TrueState.Timestamp = tag.Block.Timestamp;
                                node.FalseState.Value = new LocalizedText(tag.Labels[1]);
                                node.FalseState.Timestamp = tag.Block.Timestamp;
                            }
                        }

                        break;
                    }

                case UnderlyingSystemTagType.Enumerated:
                    {
                        MultiStateDiscreteState node = variable as MultiStateDiscreteState;

                        if (tag.Labels != null)
                        {
                            LocalizedText[] strings = new LocalizedText[tag.Labels.Length];

                            for (int ii = 0; ii < tag.Labels.Length; ii++)
                            {
                                strings[ii] = new LocalizedText(tag.Labels[ii]);
                            }

                            node.EnumStrings.Value = strings.ToArrayOf();
                            node.EnumStrings.Timestamp = tag.Block.Timestamp;
                        }

                        break;
                    }
            }
        }
    }
}
