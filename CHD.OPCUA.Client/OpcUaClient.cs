using System.Text.Unicode;
using CHD.OPCUA.Contracts;
using CHD.OPCUA.Contracts.Options;
using CHD.OPCUA.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using Opc.Ua.Configuration;

namespace CHD.OPCUA.Client
{
    public class OpcUaClient(ILogger<OpcUaClient> logger, IOptionsMonitor<Contracts.Options.OpcUaClientOptions> optionsMonitor) : IOpcUAClient
    {
        private Contracts.Options.OpcUaClientOptions _options => optionsMonitor.CurrentValue;

        private ITelemetryContext _telemetryContext = DefaultTelemetry.Create(c => c.SetMinimumLevel(LogLevel.Trace));
        private ApplicationInstance? _instance;
        private ApplicationConfiguration? _configuration => _instance.ApplicationConfiguration;
        private ISession _session;

        /// <summary>
        /// True while a session is attached.
        /// </summary>
        public bool IsConnected => _session is not null && _session.Connected;

        /// <summary>
        /// True between a reconnect starting and completing.
        /// </summary>

        public event EventHandler<MonitoredItemEventArgs> MonitoredItemNotification;

        public async Task StartAsync(CancellationToken cancellationToken = default)
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

            var timeout = (int?)_options.Timeout?.TotalMilliseconds ?? 60000;

            var endpointConfiguration = EndpointConfiguration.Create(_configuration);

            var discoveryClient = await DiscoveryClient.CreateAsync(_configuration, new Uri(this._options.EndpointUrl),
                endpointConfiguration, DiagnosticsMasks.All, cancellationToken);

            EndpointDescription? selectedEndpoint = null;
            IUserIdentity identity = null;
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                var pwBytes = new Span<byte>();
                _ = Utf8.FromUtf16(_options.Password, pwBytes, out _, out _);
                identity = new UserIdentity(_options.Username, pwBytes);
            }
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

            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
            // the managed session brings its own connection state machine and reconnect
            // policy, so there is no SessionReconnectHandler to wire up here.
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

        private void _session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            logger?.LogTrace($"Session changed to {e.CurrentState}");
        }
    }
}
