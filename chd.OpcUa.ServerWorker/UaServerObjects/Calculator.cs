using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.Server.ObjectSystem;

namespace chd.OpcUa.ServerWorker.UaServerObjects
{
    public class Calculator(string name) : BaseUaServerObject(name)
    {
        private int _counter = 0;
        [ObjectSystemProperty("CallCounter", CanWrite = true)]
        public int Counter { get => _counter; set => SetField(ref this._counter, value); }

        [ObjectSystemMethod]
        public async Task<double> Sum(double x, double y, CancellationToken cancellationToken)
        {
            Counter++;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            return x * y;
        }

        [ObjectSystemMethod("ResetCounter")]
        public void Reset()
        {
            Counter = 0;
        }
    }
}
