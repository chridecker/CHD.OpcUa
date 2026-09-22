using chd.OpcUa.Contracts;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Subscriptions;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace chd.OpcUa.Client.Extensions
{
    public static class EventExtensions
    {
        public static EventFieldList ToFieldList(this EventNotification notification)
        {
            return new EventFieldList
            {
                ClientHandle = notification.MonitoredItem?.ClientHandle ?? 0,
                EventFields = notification.Fields,
            };
        }
        public static NodeId FindEventType(this EventFilter filter, EventFieldList notification)
        {
            if (filter != null)
            {
                for (int ii = 0; ii < filter.SelectClauses.Count; ii++)
                {
                    SimpleAttributeOperand clause = filter.SelectClauses[ii];

                    if (clause.BrowsePath.Count == 1 && clause.BrowsePath[0] == BrowseNames.EventType)
                    {
                        return notification.EventFields[ii].TryGetValue(out NodeId nodeId) ? nodeId : NodeId.Null;
                    }
                }
            }

            return NodeId.Null;
        }
        public static async Task<BaseEventState> ConstructEventAsync(this ISession session,
            EventFilter filter,
            EventFieldList notification,
            Dictionary<NodeId, Type> knownEventTypes,
            Dictionary<NodeId, NodeId> eventTypeMappings,
            CancellationToken ct = default)
        {
            // find the event type.
            NodeId eventTypeId = FindEventType(filter, notification);

            if (eventTypeId.IsNull)
            {
                return null;
            }

            // look up the known event type.
            Type knownType = null;
            NodeId knownTypeId = NodeId.Null;

            if (eventTypeMappings.TryGetValue(eventTypeId, out knownTypeId))
            {
                knownType = knownEventTypes[knownTypeId];
            }

            // try again.
            if (knownType == null)
            {
                if (knownEventTypes.TryGetValue(eventTypeId, out knownType))
                {
                    knownTypeId = eventTypeId;
                    eventTypeMappings.TryAdd(eventTypeId, eventTypeId);
                }
            }

            // try mapping it to a known type.
            if (knownType == null)
            {
                // browse for the supertypes of the event type.
                List<ReferenceDescription> supertypes = await BrowseSuperTypesAsync(session, eventTypeId, false, ct).ConfigureAwait(false);

                // can't do anything with unknown types.
                if (supertypes == null)
                {
                    return null;
                }

                // find the first supertype that matches a known event type.
                for (int ii = 0; ii < supertypes.Count; ii++)
                {
                    NodeId superTypeId = (NodeId)supertypes[ii].NodeId;

                    if (knownEventTypes.TryGetValue(superTypeId, out knownType))
                    {
                        knownTypeId = superTypeId;
                        eventTypeMappings.TryAdd(eventTypeId, superTypeId);
                    }

                    if (!knownTypeId.IsNull)
                    {
                        break;
                    }
                }

                // can't do anything with unknown types.
                if (knownTypeId.IsNull)
                {
                    return null;
                }
            }

            // construct the event based on the known event type.
            BaseEventState e = (BaseEventState)Activator.CreateInstance(knownType, new object[] { (NodeState)null });

            // initialize the event with the values in the notification.
            e.Update(session.SystemContext, filter.SelectClauses, notification);

            // save the orginal notification.
            e.Handle = notification;

            return e;
        }

        public static async Task<List<ReferenceDescription>> BrowseAsync(this ISession session, IReadOnlyList<BrowseDescription> nodesToBrowse, CancellationToken cancellationToken)
        {
            try
            {
                List<ReferenceDescription> references = new List<ReferenceDescription>();

                while (nodesToBrowse.Count > 0)
                {
                    // start the browse operation.
                    var response = await session.BrowseAsync(
                        null,
                        null,
                        0,
                        nodesToBrowse.ToArrayOf(),
                        cancellationToken).ConfigureAwait(false);

                    var results = response.Results.ToList();
                    var diagnosticInfos = response.DiagnosticInfos.ToList();

                    ClientBase.ValidateResponse(results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

                    List<ByteString> continuationPoints = new List<ByteString>();
                    List<BrowseDescription> unprocessedOperations = new List<BrowseDescription>();

                    for (int ii = 0; ii < nodesToBrowse.Count; ii++)
                    {
                        // check for error.
                        if (StatusCode.IsBad(results[ii].StatusCode))
                        {
                            // this error indicates that the server does not have enough simultaneously active
                            // continuation points. This request will need to be resent after the other operations
                            // have been completed and their continuation points released.
                            if (results[ii].StatusCode == StatusCodes.BadNoContinuationPoints)
                            {
                                unprocessedOperations.Add(nodesToBrowse[ii]);
                            }

                            continue;
                        }

                        // check if all references have been fetched.
                        if (results[ii].References.Count == 0)
                        {
                            continue;
                        }

                        // save results.
                        references.AddRange(results[ii].References);

                        // check for continuation point.
                        if (!results[ii].ContinuationPoint.IsNull)
                        {
                            continuationPoints.Add(results[ii].ContinuationPoint);
                        }
                    }

                    // process continuation points.
                    while (continuationPoints.Count > 0)
                    {
                        // continue browse operation.
                        BrowseNextResponse response2 = await session.BrowseNextAsync(
                            null,
                            false,
                            continuationPoints,
                            cancellationToken).ConfigureAwait(false);

                        results = response2.Results.ToList();
                        diagnosticInfos = response2.DiagnosticInfos.ToList();

                        ClientBase.ValidateResponse(results, continuationPoints);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);

                        List<ByteString> revisedContinuationPoints = new List<ByteString>();
                        for (int ii = 0; ii < continuationPoints.Count; ii++)
                        {
                            // check for error.
                            if (StatusCode.IsBad(results[ii].StatusCode))
                            {
                                continue;
                            }

                            // check if all references have been fetched.
                            if (results[ii].References.Count == 0)
                            {
                                continue;
                            }

                            // save results.
                            references.AddRange(results[ii].References);

                            // check for continuation point.
                            if (!results[ii].ContinuationPoint.IsNull)
                            {
                                revisedContinuationPoints.Add(results[ii].ContinuationPoint);
                            }
                        }

                        // check if browsing must continue;
                        continuationPoints = revisedContinuationPoints;
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = unprocessedOperations;
                }

                // return complete list.
                return references;
            }
            catch (Exception exception)
            {
                throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
            }
        }

        public static async Task<List<ReferenceDescription>> BrowseSuperTypesAsync(this ISession session, NodeId typeId, bool throwOnError, CancellationToken ct = default)
        {
            List<ReferenceDescription> supertypes = new List<ReferenceDescription>();

            try
            {
                // find all of the children of the field.
                BrowseDescription nodeToBrowse = new BrowseDescription();

                nodeToBrowse.NodeId = typeId;
                nodeToBrowse.BrowseDirection = BrowseDirection.Inverse;
                nodeToBrowse.ReferenceTypeId = ReferenceTypeIds.HasSubtype;
                nodeToBrowse.IncludeSubtypes = false; // more efficient to use IncludeSubtypes=False when possible.
                nodeToBrowse.NodeClassMask = 0; // the HasSubtype reference already restricts the targets to Types.
                nodeToBrowse.ResultMask = (uint)BrowseResultMask.All;

                List<ReferenceDescription> references = await session.BrowseAsync(new List<BrowseDescription>() { nodeToBrowse }, ct);

                while (references != null && references.Count > 0)
                {
                    // should never be more than one supertype.
                    supertypes.Add(references[0]);

                    // only follow references within this server.
                    if (references[0].NodeId.IsAbsolute)
                    {
                        break;
                    }

                    // get the references for the next level up.
                    nodeToBrowse.NodeId = (NodeId)references[0].NodeId;
                    references = await session.BrowseAsync(new List<BrowseDescription>() { nodeToBrowse }, ct).ConfigureAwait(false);
                }

                // return complete list.
                return supertypes;
            }
            catch (Exception exception)
            {
                if (throwOnError)
                {
                    throw new ServiceResultException(exception, StatusCodes.BadUnexpectedError);
                }

                return null;
            }
        }
        private static async Task CollectFieldsAsync(this ISession session,
            NodeId eventTypeId,
            List<SimpleAttributeOperand> eventFields,
            Dictionary<NodeId, List<QualifiedName>> foundNodes,
            CancellationToken ct = default)
        {
            List<ReferenceDescription> supertypes = await session.BrowseSuperTypesAsync(eventTypeId, false, ct).ConfigureAwait(false);

            if (supertypes == null)
            {
                return;
            }

            // process the types starting from the top of the tree.
            List<QualifiedName> parentPath = new List<QualifiedName>();

            for (int ii = supertypes.Count - 1; ii >= 0; ii--)
            {
                await CollectFieldsAsync(session, (NodeId)supertypes[ii].NodeId, parentPath, eventFields, foundNodes, ct).ConfigureAwait(false);
            }

            // collect the fields for the selected type.
            await CollectFieldsAsync(session, eventTypeId, parentPath, eventFields, foundNodes, ct).ConfigureAwait(false);
        }



        public static async Task CollectFieldsAsync(this ISession session,
            NodeId nodeId,
            List<QualifiedName> parentPath,
            List<SimpleAttributeOperand> eventFields,
            Dictionary<NodeId, List<QualifiedName>> foundNodes,
            CancellationToken ct = default)
        {
            // find all of the children of the field.
            BrowseDescription nodeToBrowse = new BrowseDescription();

            nodeToBrowse.NodeId = nodeId;
            nodeToBrowse.BrowseDirection = BrowseDirection.Forward;
            nodeToBrowse.ReferenceTypeId = ReferenceTypeIds.Aggregates;
            nodeToBrowse.IncludeSubtypes = true;
            nodeToBrowse.NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable);
            nodeToBrowse.ResultMask = (uint)BrowseResultMask.All;

            List<ReferenceDescription> children = await session.BrowseAsync(new List<BrowseDescription>() { nodeToBrowse }, ct).ConfigureAwait(false);

            if (children == null)
            {
                return;
            }

            // process the children.
            for (int ii = 0; ii < children.Count; ii++)
            {
                ReferenceDescription child = children[ii];

                if (child.NodeId.IsAbsolute)
                {
                    continue;
                }

                // construct browse path.
                List<QualifiedName> browsePath = new List<QualifiedName>(parentPath);
                browsePath.Add(child.BrowseName);

                // check if the browse path is already in the list.
                if (!ContainsPath(eventFields, browsePath))
                {
                    SimpleAttributeOperand field = new SimpleAttributeOperand();

                    field.TypeDefinitionId = ObjectTypeIds.BaseEventType;
                    field.BrowsePath = browsePath;
                    field.AttributeId = (child.NodeClass == NodeClass.Variable) ? Attributes.Value : Attributes.NodeId;

                    eventFields.Add(field);
                }

                // recusively find all of the children.
                NodeId targetId = (NodeId)child.NodeId;

                // need to guard against loops.
                if (foundNodes.TryAdd(targetId, browsePath))
                {
                    await CollectFieldsAsync(session, (NodeId)child.NodeId, browsePath, eventFields, foundNodes, ct).ConfigureAwait(false);
                }
            }
        }

        private static bool ContainsPath(List<SimpleAttributeOperand> selectClause, List<QualifiedName> browsePath)
        {
            for (int ii = 0; ii < selectClause.Count; ii++)
            {
                SimpleAttributeOperand field = selectClause[ii];

                if (field.BrowsePath.Count != browsePath.Count)
                {
                    continue;
                }

                bool match = true;

                for (int jj = 0; jj < field.BrowsePath.Count; jj++)
                {
                    if (field.BrowsePath[jj] != browsePath[jj])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return true;
                }
            }

            return false;
        }
        public static async Task<List<SimpleAttributeOperand>> ConstructSelectClausesAsync(this ISession session, CancellationToken ct,
            params NodeId[] eventTypeIds)
        {
            List<SimpleAttributeOperand> selectClauses = new List<SimpleAttributeOperand>();

            SimpleAttributeOperand operand = new SimpleAttributeOperand();

            operand.TypeDefinitionId = ObjectTypeIds.ConditionType;
            operand.AttributeId = Attributes.NodeId;
            operand.BrowsePath = new List<QualifiedName>();

            selectClauses.Add(operand);

            var foundNodes = new Dictionary<NodeId, List<QualifiedName>>();

            if (eventTypeIds != null)
            {
                for (int ii = 0; ii < eventTypeIds.Length; ii++)
                {
                    await session.CollectFieldsAsync(eventTypeIds[ii], selectClauses, foundNodes, ct).ConfigureAwait(false);
                }
            }

            else
            {
                await session.CollectFieldsAsync(ObjectTypeIds.BaseEventType, selectClauses, foundNodes, ct).ConfigureAwait(false);
            }

            return selectClauses;
        }

        private static Dictionary<NodeId, Type> CreateKnownTypes()
        {
            return new Dictionary<NodeId, Type>
            {
                [ObjectTypeIds.BaseEventType] = typeof(BaseEventState),
                [ObjectTypeIds.ConditionType] = typeof(ConditionState),
                [ObjectTypeIds.DialogConditionType] = typeof(DialogConditionState),
                [ObjectTypeIds.AlarmConditionType] = typeof(AlarmConditionState),
                [ObjectTypeIds.ExclusiveLimitAlarmType] = typeof(ExclusiveLimitAlarmState),
                [ObjectTypeIds.NonExclusiveLimitAlarmType] = typeof(NonExclusiveLimitAlarmState),
                [ObjectTypeIds.AuditEventType] = typeof(AuditEventState),
                [ObjectTypeIds.AuditUpdateMethodEventType] = typeof(AuditUpdateMethodEventState),
            };
        }

        public static async Task<EventAlarmEventArgs?> ProcessNotificationAsync(this ISession session, Dictionary<uint, ConditionState> conditionStates, Dictionary<uint, EventFilter> filterByHandle, EventNotification notification, CancellationToken ct)
        {
            uint clientHandle = notification.MonitoredItem?.ClientHandle ?? 0;

            if (!filterByHandle.TryGetValue(clientHandle, out EventFilter filter))
            {
                return null;
            }

            var fields = notification.ToFieldList();

            var eventTypeId = filter.FindEventType(fields);
            if (eventTypeId.IsNull)
            {
                return null;
            }

            // a refresh starts the list over and ends without anything to show
            if (eventTypeId == ObjectTypeIds.RefreshStartEventType)
            {

                if (conditionStates.ContainsKey(clientHandle))
                {
                    conditionStates.Remove(clientHandle);
                }
                return null;
            }

            if (eventTypeId == ObjectTypeIds.RefreshEndEventType)
            {
                return null;
            }

            var d = new Dictionary<NodeId, NodeId>();
            // construct the condition object.
            var condition = await session.ConstructEventAsync(
                filter,
                fields,
                CreateKnownTypes(),
                d,
                ct).ConfigureAwait(false) as ConditionState;

            if (condition is null)
            {
                return null;
            }

            conditionStates[clientHandle] = condition;

            INode type = await session.NodeCache.FindAsync(condition.TypeDefinitionId, ct).ConfigureAwait(false);


            return new EventAlarmEventArgs()
            {
                Id = condition.EventId.Value.Memory,
                Handle = clientHandle,
                Type = type?.ToString(),
                SourceName = condition.SourceName?.Value,
                ConditionName = condition.ConditionName?.Value,
                Time = condition.Time.Value.ToDateTime(),
                Severity = condition.Severity.Value,
                StateText = condition.EnabledState?.EffectiveDisplayName?.Value.Text,
                Message = condition.Message?.Value.Text,
                Comment = condition.Comment?.Value.Text,
                Retain = condition.Retain.Value,
                IsDialog = condition is DialogConditionState,
                DialogText = condition is DialogConditionState dialog ? dialog.Prompt.Value.Text : string.Empty,
                IsAlarm = condition is AlarmConditionState,
                CanSilence = condition is AlarmConditionState alarm && !(alarm.SilenceState?.Id?.Value ?? false),
                DialogResponses = condition is DialogConditionState dialog1 ? dialog1.ResponseOptionSet.Value
                    .ToArray()
                    .Select(option => Utils.Format("{0}", option))
                    .ToArray() : new[] { string.Empty }
            };
        }

        public static EventFilter ConstructFilter(this List<SimpleAttributeOperand> clauses, EventSeverity severity = EventSeverity.Min, bool ignoreSuppressedOrShelved = false)
        {
            var filter = new EventFilter();
            filter.SelectClauses = clauses.ToArrayOf();

            var whereClause = new ContentFilter();
            // add the severity.
            ContentFilterElement element1 = null;
            ContentFilterElement element2 = null;

            if (severity > EventSeverity.Min)
            {
                // select the Severity property of the event.
                SimpleAttributeOperand operand1 = new SimpleAttributeOperand();
                operand1.TypeDefinitionId = ObjectTypeIds.BaseEventType;
                operand1.BrowsePath = new List<QualifiedName> { new QualifiedName(BrowseNames.Severity) }.ToArrayOf();
                operand1.AttributeId = Attributes.Value;

                // specify the value to compare the Severity property with.
                LiteralOperand operand2 = new LiteralOperand();
                operand2.Value = new Variant((ushort)severity);

                // specify that the Severity property must be GreaterThanOrEqual the value specified.
                element1 = whereClause.Push(FilterOperator.GreaterThanOrEqual, new Variant(new ExtensionObject(operand1)), new Variant(new ExtensionObject(operand2)));
            }

            // add the suppressed or shelved.
            if (!ignoreSuppressedOrShelved)
            {
                // select the SuppressedOrShelved property of the event.
                SimpleAttributeOperand operand1 = new SimpleAttributeOperand();
                operand1.TypeDefinitionId = ObjectTypeIds.BaseEventType;
                operand1.BrowsePath = new List<QualifiedName> { new QualifiedName(BrowseNames.SuppressedOrShelved) }.ToArrayOf();
                operand1.AttributeId = Attributes.Value;

                // specify the value to compare the Severity property with.
                LiteralOperand operand2 = new LiteralOperand();
                operand2.Value = new Variant(false);

                // specify that the Severity property must Equal the value specified.
                element2 = whereClause.Push(FilterOperator.Equals, new Variant(new ExtensionObject(operand1)), new Variant(new ExtensionObject(operand2)));

                // SuppressedOrShelved is declared by AlarmConditionType, so an event of a
                // condition which is not an alarm - the OnlineState dialog of a source, for
                // one - carries no such field. An operand which resolves to nothing makes
                // Equals answer null and the element false, so the clause on its own would
                // silently drop every non alarm condition. Asking it only of the alarms is
                // what keeps the dialogs of the sample in the list.
                LiteralOperand operand3 = new LiteralOperand();
                operand3.Value = new Variant(ObjectTypeIds.AlarmConditionType);

                ContentFilterElement isAlarm = whereClause.Push(FilterOperator.OfType, new Variant(new ExtensionObject(operand3)));
                ContentFilterElement notAnAlarm = whereClause.Push(FilterOperator.Not, new Variant(new ExtensionObject(isAlarm)));

                element2 = whereClause.Push(FilterOperator.Or, new Variant(new ExtensionObject(notAnAlarm)), new Variant(new ExtensionObject(element2)));

                // chain multiple elements together with an AND clause.
                if (element1 != null)
                {
                    element1 = whereClause.Push(FilterOperator.And, new Variant(new ExtensionObject(element1)), new Variant(new ExtensionObject(element2)));
                }
                else
                {
                    element1 = element2;
                }
            }
            var eventTypes = new List<NodeId> { ObjectTypeIds.ConditionType };

            // add the event types.
            if (eventTypes != null && eventTypes.Count > 0)
            {
                element2 = null;

                // save the last element.
                for (int ii = 0; ii < eventTypes.Count; ii++)
                {
                    // for this example uses the 'OfType' operator to limit events to thoses with specified event type.
                    LiteralOperand operand1 = new LiteralOperand();
                    operand1.Value = new Variant(eventTypes[ii]);
                    ContentFilterElement element3 = whereClause.Push(FilterOperator.OfType, new Variant(new ExtensionObject(operand1)));

                    // need to chain multiple types together with an OR clause.
                    if (element2 != null)
                    {
                        element2 = whereClause.Push(FilterOperator.Or, new Variant(new ExtensionObject(element2)), new Variant(new ExtensionObject(element3)));
                    }
                    else
                    {
                        element2 = element3;
                    }
                }

                // need to link the set of event types with the previous filters.
                if (element1 != null)
                {
                    element1 = whereClause.Push(FilterOperator.And, new Variant(new ExtensionObject(element1)), new Variant(new ExtensionObject(element2)));
                }
                else
                {
                    element1 = element2;
                }
            }

            // Part 9 frames a condition refresh with a RefreshStart and a RefreshEnd event,
            // and a client relies on the first of the two to start its list over. The
            // stack runs those two through the where clause like any other event, and a
            // filter which asks for conditions - or for a severity - drops them: they are
            // system events which carry neither. So they are asked for explicitly, next
            // to whatever else the filter asks for.
            if (element1 != null)
            {
                ContentFilterElement markers = null;

                foreach (NodeId markerTypeId in new[] { ObjectTypeIds.RefreshStartEventType, ObjectTypeIds.RefreshEndEventType })
                {
                    LiteralOperand operand = new LiteralOperand();
                    operand.Value = new Variant(markerTypeId);
                    ContentFilterElement marker = whereClause.Push(FilterOperator.OfType, new Variant(new ExtensionObject(operand)));

                    markers = markers == null
                        ? marker
                        : whereClause.Push(FilterOperator.Or, new Variant(new ExtensionObject(markers)), new Variant(new ExtensionObject(marker)));
                }

                whereClause.Push(FilterOperator.Or, new Variant(new ExtensionObject(element1)), new Variant(new ExtensionObject(markers)));
            }

            filter.WhereClause = whereClause;

            // return filter.
            return filter;
        }
    }
}
