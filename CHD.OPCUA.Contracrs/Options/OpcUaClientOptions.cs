using System;
using System.Collections.Generic;
using System.Text;

namespace CHD.OPCUA.Contracts.Options
{
    public class OpcUaClientOptions
    {
        public string Name { get; set; }
        public string EndpointUrl { get; set; }
        public TimeSpan? Timeout { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseCertificate { get; set; }
    }
}
