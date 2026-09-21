using CHD.OPCUA.Contracts;
using CHD.OPCUA.Contracts.Interfaces;
using CHD.OPCUA.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using Opc.Ua.Configuration;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Text.Unicode;
using System.Xml.Linq;

namespace CHD.OPCUA.Client
{
    public class OpcUaClient(ILogger<OpcUaClient> logger, IOptionsMonitor<Contracts.Options.OpcUaClientOptions> optionsMonitor) : IOpcUAClient
    {
        private Contracts.Options.OpcUaClientOptions _options => optionsMonitor.CurrentValue;
        private ITelemetryContext _telemetryContext = DefaultTelemetry.Create(c => c.SetMinimumLevel(LogLevel.Trace));
        private ApplicationInstance? _instance;
        private ApplicationConfiguration? _configuration => _instance.ApplicationConfiguration;

        private List<ExpandedNodeId> _nodes = [];
        private List<ExpandedNodeId> _methods = [];

        private ISession _session;

        public bool IsConnected => _session is not null && _session.Connected;

        public event EventHandler<MonitoredItemEventArgs> MonitoredItemNotification;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var timeout = (int?)_options.Timeout?.TotalMilliseconds ?? 60000;
            await InitializeApplicationInstance(cancellationToken);

            var identity = GetIdentity();

            var endpoint = await GetEndpointAsync(identity, cancellationToken);

            await CreateSessionAsync(endpoint, timeout, identity, cancellationToken);

            await BrowseNodeAsync(ObjectIds.ObjectsFolder, cancellationToken);
        }

        public Task<T> ReadAsync<T>(string node, CancellationToken cancellationToken)
            => ExecuteForNode(node, (n) => _session.ReadValueAsync<T>(n, cancellationToken));

        public Task<bool> WriteAsync<T>(string node, T value, CancellationToken cancellationToken)
            => ExecuteForNode(node, async (n) =>
            {
                var nodesToRead = new List<ReadValueId> {
                    new ReadValueId { NodeId = n, AttributeId = Attributes.Value },
                };

                var results = await _session.ReadAsync(null, 0, TimestampsToReturn.Neither, nodesToRead, cancellationToken);
                var dataValue = results.Results[0];
                var writeValue = new WriteValue()
                {
                    NodeId = n,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(ChangeType(dataValue, value), StatusCodes.Good, DateTime.MinValue, DateTime.MinValue)
                };
                var res = await _session.WriteAsync(null, new[] { writeValue }, cancellationToken);
                return res.Results.ToList().All(a => StatusCode.IsGood(a));
            });

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_session is null)
            {
                return;
            }
            _session.KeepAlive -= _session_KeepAlive;
            if (_session is ManagedSession managedSession)
            {
                managedSession.ConnectionStateChanged -= OnConnectionStateChanged;
            }

            _ = await _session.CloseAsync(cancellationToken);
        }

        private Task<T> ExecuteForNode<T>(string node, Func<NodeId, Task<T>> func)
        {
            if (_nodes.Any(a => a.IdentifierAsString == node))
            {
                var cachedNode = _nodes.FirstOrDefault(a => a.IdentifierAsString == node);
                return func(cachedNode.InnerNodeId);
            }
            throw new Exception($"Konten {node} nicht gefunden!");
        }

        private async Task BrowseNodeAsync(NodeId? parentId, CancellationToken cancellationToken)
        {
            parentId ??= ObjectIds.ObjectsFolder;
            var nodesToBrowse = new List<BrowseDescription> {
                    // the components of the node.
                    new BrowseDescription {
                        NodeId = parentId.Value,
                        BrowseDirection = BrowseDirection.Forward,
                        ReferenceTypeId = ReferenceTypeIds.Aggregates,
                        IncludeSubtypes = true,
                        NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                        ResultMask = (uint)BrowseResultMask.All,
                    },
                    // the nodes organized by the node.
                    new BrowseDescription {
                        NodeId = parentId.Value,
                        BrowseDirection = BrowseDirection.Forward,
                        ReferenceTypeId = ReferenceTypeIds.Organizes,
                        IncludeSubtypes = true,
                        NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                        ResultMask = (uint)BrowseResultMask.All,
                    },
                };
            foreach (var child in await BrowseAsync(nodesToBrowse, cancellationToken))
            {
                switch (child.NodeClass)
                {
                    case NodeClass.Method:
                        // register Method
                        _methods.Add(child.NodeId);
                        break;
                    case NodeClass.Variable:
                        _nodes.Add(child.NodeId);
                        break;
                    default:
                        await BrowseNodeAsync(child.NodeId.InnerNodeId, cancellationToken);
                        break;
                }
            }

            // update the attributes display.

        }


        private async Task<List<ReferenceDescription>> BrowseAsync(IReadOnlyList<BrowseDescription> nodesToBrowse, CancellationToken cancellationToken)
        {
            try
            {
                List<ReferenceDescription> references = new List<ReferenceDescription>();

                while (nodesToBrowse.Count > 0)
                {
                    // start the browse operation.
                    var response = await _session.BrowseAsync(
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
                        BrowseNextResponse response2 = await _session.BrowseNextAsync(
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


        private async Task InitializeApplicationInstance(CancellationToken cancellationToken)
        {
            if (_instance is null)
            {
                _instance = new ApplicationInstance(_telemetryContext)
                {
                    ApplicationType = ApplicationType.Client,
                    ApplicationName = _options.Name,
                };
                var configFile = new FileInfo("OpcUaClientConfig.xml");
                if (!configFile.Exists)
                {
                    throw new FileNotFoundException("Opc UA Client konnte nicht gefunden werden!", configFile.FullName);
                }
                _ = await _instance.LoadApplicationConfigurationAsync(configFile.FullName, false, cancellationToken);

                if (await _instance.CheckApplicationInstanceCertificatesAsync(false, ct: cancellationToken))
                {
                    _instance.CertificateManager.AutoAcceptUntrustedCertificates = true;
                }
            }
        }

        private IUserIdentity? GetIdentity()
        {
            IUserIdentity identity = null;
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                var pwBytes = new Span<byte>();
                _ = Utf8.FromUtf16(_options.Password, pwBytes, out _, out _);
                identity = new UserIdentity(_options.Username, pwBytes);
            }

            return identity;
        }

        private async Task CreateSessionAsync(ConfiguredEndpoint endpoint, int timeout, IUserIdentity identity, CancellationToken cancellationToken)
        {
            _session = await new ManagedSessionFactory(_telemetryContext).CreateAsync(
                _configuration,
                endpoint,
                false,
                true,
                !string.IsNullOrWhiteSpace(_options.Name) ? _options.Name : nameof(OpcUaClient),
                (uint)timeout,
                identity ?? new UserIdentity(), new string[] { },
                cancellationToken);

            _session.KeepAlive += _session_KeepAlive;

            if (_session is ManagedSession managedSession)
            {
                managedSession.ConnectionStateChanged += OnConnectionStateChanged;
            }

            try
            {
                using var typeSystem = ComplexTypeSystemClientExtensions.Create(_session, _telemetryContext);

                await typeSystem.LoadAsync(ct: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // the session is usable without the custom types; a sample which needs them
                // fails later, with an error which says which type is missing
                logger?.LogWarning(e, "Failed to load complex type system.");
            }
        }

        private async Task<ConfiguredEndpoint> GetEndpointAsync(IUserIdentity identity, CancellationToken cancellationToken)
        {
            var endpointConfiguration = EndpointConfiguration.Create(_configuration);

            var discoveryClient = await DiscoveryClient.CreateAsync(_configuration, new Uri(this._options.EndpointUrl),
                endpointConfiguration, DiagnosticsMasks.All, cancellationToken);

            EndpointDescription? selectedEndpoint = null;

            foreach (var ep in (await discoveryClient.GetEndpointsAsync(new string[] { }, cancellationToken)).ToList())
            {
                if (identity is not null
                    && _options.UseCertificate
                    && ep.SecurityMode is MessageSecurityMode.SignAndEncrypt)
                {
                    selectedEndpoint = ep;
                    break;
                }
                if (identity is not null
                    && !_options.UseCertificate
                    && ep.SecurityMode is MessageSecurityMode.Sign)
                {
                    selectedEndpoint = ep;
                    break;
                }
                if (identity is null
                    && !_options.UseCertificate
                    && ep.SecurityMode is MessageSecurityMode.None)
                {
                    selectedEndpoint = ep;
                    break;
                }
            }

            if (selectedEndpoint is null)
            {
                throw new Exception($"Konnte keinen validen Endpoint auf {_options.EndpointUrl} finden!");
            }

            return new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            // the event may be raised by the session or by the connection state machine
            // behind it, so only a sender which is a session is worth comparing.
            if (_session is null || (sender is ISession sessionOfEvent && !ReferenceEquals(sessionOfEvent, _session)))
            {
                return;
            }

            switch (e.NewState)
            {
                case ConnectionState.Reconnecting:
                case ConnectionState.Failover:
                    {
                        logger?.LogWarning($"Reconnecting (attempt {0})", e.ReconnectAttempt);
                        break;
                    }

                case ConnectionState.Connected:
                    {
                        logger?.LogDebug($"Session Connected {_session.Endpoint.EndpointUrl}");
                        break;
                    }

                case ConnectionState.Disconnected:
                    {
                        logger?.LogError($"Session Disconnected {e.Error}");
                        break;
                    }
            }
        }

        private Variant ChangeType(DataValue value, object newValue)
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

        private void _session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            logger?.LogTrace($"Session changed to {e.CurrentState}");
        }


    }
}
