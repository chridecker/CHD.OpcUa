using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemMethod : UnderlyingSystemBase
    {
        private readonly Func<UnderlyingSystemMethod, object[], CancellationToken, ValueTask<object[]>> _execution;

        public override string Identifier => this.Block.Identifier + "#" + Name;
        public bool CanExecute { get; }

        public UnderlyingSystemBlock Block { get; set; }

        public List<UnderlyingSystemMethodArgument> InputArguments { get; } = [];
        public List<UnderlyingSystemMethodArgument> OutputArguments { get; } = [];

        public UnderlyingSystemMethod(string name,string description,bool canExecute, UnderlyingSystemBlock block, Func<UnderlyingSystemMethod, object[], CancellationToken, ValueTask<object[]>> execution)
        :base(name)
        {
            _execution = execution;
            Block = block;
            Description = description;
            CanExecute = canExecute;
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
            catch (ArgumentOutOfRangeException ex)
            {
                return ServiceResult.Create(ex, StatusCodes.BadInvalidArgument, string.Empty);
            }
            catch (Exception ex)
            {
                return ServiceResult.Create(ex, StatusCodes.BadUnexpectedError, string.Empty);
            }
        }

        public UnderlyingSystemMethod CreateSnapshot() => (UnderlyingSystemMethod)MemberwiseClone();
    }
}
