using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.Interfaces
{
    public interface INamespaceManager
    {
        string[] NameSpaces { get; }
    }
}
