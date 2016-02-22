using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Phenomes;

namespace UnitTests
{
    class BlackBoxDummy : IBlackBox
    {
        public int InputCount { get; }
        public int OutputCount { get; }

        private ISignalArray _inputSignalArray;
        public ISignalArray InputSignalArray => _inputSignalArray;

        private ISignalArray _outputSignalArray;
        public ISignalArray OutputSignalArray
        {
            get
            {
                _outputSignalArray.CopyFrom(OutputValues[Step], 0);
                return _outputSignalArray;
            }
        }

        public bool IsStateValid => true;

        public int Step { get; private set; }
        public double[][] OutputValues { get; private set; }

        private bool _isReset = true;

        public BlackBoxDummy(int inputCount, int outputCount)
        {
            InputCount = inputCount;
            OutputCount = outputCount;
            _inputSignalArray = new SignalArray(new double[inputCount + 1], 0, inputCount + 1); // +1 for bias
            _outputSignalArray = new SignalArray(new double[outputCount], 0, outputCount);
            Step = -1;
        }

        public void SetOutputValues(double[][] values)
        {
            OutputValues = values;
        }

        public void Activate()
        {
            if (!_isReset)
            {
                throw new Exception("BlackBoxDummy is not reset");
            }
            if (Step >= OutputValues.Length)
            {
                throw new Exception($"BlackBoxDummy recieved {Step + 1} calls, but expected: {OutputValues.Length} calls");
            }
            _isReset = false;
        }

        public void ResetState()
        {
            Step++;
            _isReset = true;
        }
    }
}
