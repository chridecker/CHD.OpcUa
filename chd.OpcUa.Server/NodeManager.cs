using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.Server.Model;
using chd.OpcUa.Server.UnderlyingSystem;
using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Server.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace chd.OpcUa.ServerWorker
{
    public class NodeManager : FluentNodeManagerBase
    {
        private readonly IUnderlyingSystemManager _underlyingSystemManager;
        private NodeIdDictionary<BlockState> _blocks = new();
        private NodeIdDictionary<MethodExecutionState> _methods = new();

        public IUnderlyingSystemManager UnderlyingSystemManager => this._underlyingSystemManager;

        public NodeManager(IServerInternal server, ApplicationConfiguration configuration, IUnderlyingSystemManager underlyingSystemManager, params string[] namespaces)
            : base(server, configuration, server.Telemetry.CreateLogger<NodeManager>(), namespaces)
        {
            _underlyingSystemManager = underlyingSystemManager;
            this.AliasRoot = "CHD";
        }

        public override NodeId New(ISystemContext context, NodeState node)
            => ModelUtils.ConstructIdForComponent(node, NamespaceIndex);

        public override async ValueTask CreateAddressSpaceAsync(
            IDictionary<NodeId, IList<IReference>> externalReferences,
            CancellationToken cancellationToken = default)
        {
            await base.CreateAddressSpaceAsync(externalReferences, cancellationToken).ConfigureAwait(false);

            foreach (var segment in await _underlyingSystemManager.GetMainSegmentsAsync(cancellationToken))
            {
                IList<IReference> references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                var segmentId = ModelUtils.ConstructIdForSegment(segment.Identifier, NamespaceIndex);
                var node = new NodeStateReference(ReferenceTypeIds.Organizes, false, segmentId);
                references.Add(node);
            }

            var builder = CreateFluentBuilder(NamespaceIndex);

            builder.ResolveNodes(IsSegmentOrBlockId, ResolveSegmentOrBlockAsync)
                .OnMonitoredItemCreated(OnBlockMonitoredItemCreated)
                .OnMonitoredItemDeleted(OnBlockMonitoredItemDeletedAsync);

            await RegisterAuthoredNodesAsync(builder, cancellationToken).ConfigureAwait(false);
            await CompleteConfigureAsync(externalReferences, cancellationToken).ConfigureAwait(false);
            await SealConfigurationAsync(builder, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsSegmentOrBlockId(NodeId nodeId) => nodeId.IdType is IdType.String;

        private async ValueTask<NodeState> ResolveSegmentOrBlockAsync(ISystemContext context, NodeId nodeId, CancellationToken cancellationToken)
        {
            var parsedNodeId = ParsedNodeId.Parse(nodeId);

            if (parsedNodeId is null)
            {
                return default;
            }

            NodeState root = null;

            if (parsedNodeId.RootType == ModelUtils.Segment)
            {
                var segment = await _underlyingSystemManager.FindSegmentByIdentifier(parsedNodeId.RootId, cancellationToken);

                if (segment is null)
                {
                    return default;
                }

                var rootId = ModelUtils.ConstructIdForSegment(segment.Identifier, NamespaceIndex);

                root = new SegmentState(rootId, segment);
            }

            else if (parsedNodeId.RootType == ModelUtils.Block)
            {
                var block = await _underlyingSystemManager.FindBlockByIdentifier(parsedNodeId.RootId, cancellationToken);

                if (block is null)
                {
                    return default;
                }

                var rootId = ModelUtils.ConstructIdForBlock(block.Identifier, NamespaceIndex);

                if (_blocks.TryGetValue(rootId, out BlockState node))
                {
                    root = node;
                }
                else
                {
                    root = new BlockState(this, rootId, block);
                }
            }
            else if (parsedNodeId.RootType == ModelUtils.Method)
            {
                var method =
                    await _underlyingSystemManager.FindMethodByIdentifier(parsedNodeId.RootId, cancellationToken);
                if (method is null)
                {
                    return default;
                }
                var rootId = ModelUtils.ConstructIdForMethod(method.Identifier, NamespaceIndex);
                if (_methods.TryGetValue(rootId, out var methodState))
                {
                    root = methodState;
                }
                else
                {
                    root = new MethodExecutionState(this, rootId, method, null);
                    this._methods.Add(rootId, (MethodExecutionState)root);
                }
            }
            else if (parsedNodeId.RootType == ModelUtils.InputArgument)
            {
                var method =
                    await _underlyingSystemManager.FindMethodByIdentifier(parsedNodeId.RootId.Replace(":Input", ""), cancellationToken);
                if (method is null)
                {
                    return default;
                }
                var rootId = ModelUtils.ConstructIdForMethod(method.Identifier, NamespaceIndex);

                if (_methods.TryGetValue(rootId, out var methodState))
                {
                    root = methodState.InputArguments;
                }
            }
            else if (parsedNodeId.RootType == ModelUtils.OutputArgument)
            {
                var method =
                    await _underlyingSystemManager.FindMethodByIdentifier(parsedNodeId.RootId.Replace(":Output", ""), cancellationToken);
                if (method is null)
                {
                    return default;
                }
                var rootId = ModelUtils.ConstructIdForMethod(method.Identifier, NamespaceIndex);
                if (_methods.TryGetValue(rootId, out var methodState))
                {
                    root = methodState.OutputArguments;
                }
            }
            else
            {
                return default;
            }

            if (string.IsNullOrEmpty(parsedNodeId.ComponentPath))
            {
                return root;
            }
            return root.FindChildBySymbolicName(context, parsedNodeId.ComponentPath);
        }



        private void OnBlockMonitoredItemCreated(
            ISystemContext context,
            NodeState source,
            ISampledDataChangeMonitoredItem monitoredItem)
        {
            if (source.GetHierarchyRoot() is BlockState block)
            {
                block.StartMonitoring((ServerSystemContext)context);
                _blocks[block.NodeId] = block;
            }
        }

        private ValueTask OnBlockMonitoredItemDeletedAsync(
            ISystemContext context,
            NodeState source,
            ISampledDataChangeMonitoredItem monitoredItem,
            CancellationToken cancellationToken)
        {
            if (source.GetHierarchyRoot() is BlockState block &&
                !block.StopMonitoring((ServerSystemContext)context))
            {
                _blocks.TryRemove(block.NodeId, out _);
            }

            return default;
        }
    }
}
