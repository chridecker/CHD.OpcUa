using chd.OpcUa.Server.UnderlyingSystem;
using chd.OpcUa.ServerWorker;
using Opc.Ua;
using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Base.Extensions;
using LocalizedText = Opc.Ua.LocalizedText;

namespace chd.OpcUa.Server.Model
{
    public abstract class PropertyArgumentState : PropertyState<ArrayOf<Argument>>.Implementation<StructureBuilder<Argument>>
    {
        private readonly UnderlyingSystemMethod _method;

        protected PropertyArgumentState(UnderlyingSystemMethod method, NodeId nodeId, string name, Func<UnderlyingSystemMethod, List<UnderlyingSystemMethodArgument>> argFunc, NodeState? parent) : base(parent)
        {
            _method = method;
            NodeId = nodeId;
            BrowseName = new QualifiedName(name);
            DisplayName = new LocalizedText(name);
            TypeDefinitionId = VariableTypeIds.PropertyType;
            ReferenceTypeId = ReferenceTypeIds.HasProperty;
            DataType = DataTypeIds.Argument;
            ValueRank = ValueRanks.OneDimension;

            var definedArgs = argFunc(_method);

            var args = new Argument[definedArgs.Count];
            for (int i = 0; i < definedArgs.Count; i++)
            {
                var arg = definedArgs[i];
                args[i] = new Argument(arg.Name, arg.Type.GetDataType(), ValueRanks.Scalar, arg.Description);
            }
            this.Value = args.ToArrayOf();
        }
    }
}
