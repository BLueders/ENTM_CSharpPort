using System;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using ENTM.Utility;

namespace ENTM.TuringMachine
{
    public class TuringController : IController
    {
        public IBlackBox BlackBox { get; set; }

        public ITuringMachine TuringMachine { get; }

        public TuringController(TuringMachineProperties props)
        {
            TuringMachine = new MinimalTuringMachine(props);
        }

        private double[] _controllerInput;

        public double[] ActivateNeuralNetwork(double[] environmentInput, double[] controllerInput)
        {
            if (BlackBox == null) throw new ArgumentNullException("BlackBox was null! Remember to set the BlackBox object before activating");

            double[] input = Utilities.JoinArrays(environmentInput, controllerInput);

            // Cap values at 1
            Utilities.ClampArray01(input); // FIXME: This might be a symptom in the GTM.

            Debug.Log($"Neural Network Input:  {Utilities.ToString(input, "f4")}", true);

            BlackBox.ResetState();
            BlackBox.InputSignalArray.CopyFrom(input, 0, input.Length);
            BlackBox.Activate();

            double[] output = new double[BlackBox.OutputSignalArray.Length];
            BlackBox.OutputSignalArray.CopyTo(output, 0);

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
            TuringMachine.Reset();
        }

        public double[] NoveltyVector => TuringMachine.NoveltyVector;
    }
}
