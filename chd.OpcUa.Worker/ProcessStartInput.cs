using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Worker
{
    public struct ProcessStartInput
    {
        public uint InitalState { get; set; }
        public uint FinalState { get; set; }

        public ProcessStartInput(uint initalState, uint finalState)
        {
            InitalState = initalState;
            FinalState = finalState;
        }
    }
}
