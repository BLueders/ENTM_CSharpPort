using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.CopyTask;
using SharpNeat.Phenomes;

namespace ENTM.TuringMachine
{
    class TuringController : IController
    {
        private readonly IBlackBox _blackBox;

        public ITuringMachine TuringMachine { get; }

        public TuringController(IBlackBox blackBox, CopyTaskProperties props)
        {
            TuringMachine = new MinimalTuringMachine(props);
            _blackBox = blackBox;
        }

        public double[] ActivateNeuralNetwork(double[] enviromentInput, double[] controllerInput)
        {
            double[] input = new double[enviromentInput.Length + controllerInput.Length + 1];
            Array.Copy(enviromentInput, input, enviromentInput.Length);
            Array.Copy(controllerInput, 0, input, enviromentInput.Length, controllerInput.Length);
            input[input.Length - 1] = 1.0; // Bias node

            // Cap values at 1
            Utilities.ClampArray01(input); // FIXME: This might be a symptom in the GTM.

            _blackBox.ResetState();
            for (int i = 0; i < input.Length; i++)
            {
                _blackBox.InputSignalArray[i] = input[i];
            }
            _blackBox.Activate();
            double[] output = new double[_blackBox.OutputSignalArray.Length];
            _blackBox.OutputSignalArray.CopyTo(output, 0);
            return output;
        }


        public double[] ProcessNNOutputs(double[] fromNN)
        {
           return Utilities.Flatten(TuringMachine.ProcessInput(fromNN));
        }

        public double[] InitialInput => Utilities.Flatten(TuringMachine.GetDefaultRead());

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
