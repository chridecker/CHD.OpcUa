using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemSegment
    {
        public string Name { get; set; }

        public UnderlyingSystemSegment? Parent { get; set; }

        public IList<UnderlyingSystemSegment> Children { get; set; } = [];

        public IList<string> Blocks { get; set; } = [];

        public string Identifier
        {
            get
            {
                var names = new Stack<string>();

                UnderlyingSystemSegment? current = this;

                while (current is not null)
                {
                    names.Push(current.Name);
                    current = current.Parent;
                }

                return string.Join(".", names);
            }
        }

        public UnderlyingSystemSegment(string name, UnderlyingSystemSegment? parent = null)
        {
            Name = name;
            Parent = parent;
        }
    }
}
