using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Contracts.Interfaces
{
    public interface ISystemElement
    {
        string Name { get; }
        string Description { get; set; }
        string Identifier { get; }
    }
}
