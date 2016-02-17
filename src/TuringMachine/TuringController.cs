using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.CopyTask;
using SharpNeat.Phenomes;
using ENTM.Utility;

namespace ENTM.TuringMachine
{
    class TuringController : IController
    {
        private readonly IBlackBox _blackBox;

        public ITuringMachine TuringMachine { get; }

        public TuringController(IBlackBox blackBox, TuringMachineProperties props)
        {
            TuringMachine = new MinimalTuringMachine(props);
            _blackBox = blackBox;
        }

        public double[] ActivateNeuralNetwork(double[] environmentInput, double[] controllerInput)
        {
            //double[] input = new double[environmentInput.Length + controllerInput.Length + 1];
            double[] input = new double[environmentInput.Length + controllerInput.Length];
            Array.Copy(environmentInput, input, environmentInput.Length);
            Array.Copy(controllerInput, 0, input, environmentInput.Length, controllerInput.Length);
            //input[input.Length - 1] = 1.0; // Bias node

            // Cap values at 1
            Utilities.ClampArray01(input); // FIXME: This might be a symptom in the GTM.

            Debug.Log($"Neural Network Input:  {Utilities.ToString(input, "f4")}", true);

            _blackBox.ResetState();
            for (int i = 0; i < input.Length; i++)
            {
                _blackBox.InputSignalArray[i] = input[i];
            }
            _blackBox.Activate();
            double[] output = new double[_blackBox.OutputSignalArray.Length];
            _blackBox.OutputSignalArray.CopyTo(output, 0);

            Debug.Log($"Neural Network Output: {Utilities.ToString(output, "f4")}", true);

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
