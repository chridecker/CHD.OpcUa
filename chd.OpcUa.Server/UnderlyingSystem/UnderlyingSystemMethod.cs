using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemMethod
    {
        private readonly Func<UnderlyingSystemMethod, object[], CancellationToken, ValueTask<object[]>> _execution;
        public string Identifier => this.Block.Identifier + "#" + Name;
        public string Name { get; set; }
        public UnderlyingSystemBlock Block { get; set; }

        public List<UnderlyingSystemMethodArgument> InputArguments { get; } = [];
        public List<UnderlyingSystemMethodArgument> OutputArguments { get; } = [];

        public UnderlyingSystemMethod(string name, UnderlyingSystemBlock block, Func<UnderlyingSystemMethod, object[], CancellationToken, ValueTask<object[]>> execution)
        {
            _execution = execution;
            Name = name;
            Block = block;
        }

        public void CreateInputArgument(string name, Type type) => InputArguments.Add(new(this, name, type));
        public void CreateOutputArgument(string name, Type type) => OutputArguments.Add(new(this, name, type));

        public async ValueTask<ServiceResult> ExecuteAsync(object[] inputs, List<Variant> output, CancellationToken cancellationToken)
        {
            try
            {
                var results = await _execution(this, inputs, cancellationToken);
                for (int i = 0; i < results.Length; i++)
                {
                    output[i] = new Variant(results[i]);
                }

                return ServiceResult.Good;
            }
            catch (NotImplementedException ex)
            {
                return ServiceResult.Create(ex, StatusCodes.BadMethodInvalid, string.Empty);
            }
            catch (Exception ex)
            {
                return ServiceResult.Create(ex, StatusCodes.BadUnexpectedError, string.Empty);
            }
        }

        public UnderlyingSystemMethod CreateSnapshot() => (UnderlyingSystemMethod)MemberwiseClone();
    }
}
