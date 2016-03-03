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
            _turingInputLength = TuringMachine.InputCount;
        }

        private readonly int _turingInputLength;
        private double[] _turingMachineOutput;

        public double[] ActivateNeuralNetwork(double[] environmentOutput)
        {
            if (BlackBox == null)
            {
                Console.WriteLine("BlackBox was null! Remember to set the BlackBox object before activating");
                return null;
                //throw new ArgumentNullException("BlackBox was null! Remember to set the BlackBox object before activating");
            }

            // NN Input is the output from the environment, and the output from the turing machine in the previous activation
            double[] nnInput = Utilities.JoinArrays(environmentOutput, _turingMachineOutput);

            // Cap values at 1
            Utilities.ClampArray01(nnInput); // FIXME: This might be a symptom in the GTM.

            Debug.Log($"Neural Network Input:  {Utilities.ToString(nnInput, "f4")}", true);

            // Activate the neural network
            BlackBox.ResetState();
            BlackBox.InputSignalArray.CopyFrom(nnInput, 0, nnInput.Length);
            BlackBox.Activate();

            double[] nnOutput = new double[BlackBox.OutputSignalArray.Length];
            BlackBox.OutputSignalArray.CopyTo(nnOutput, 0);

            Debug.Log($"Neural Network Output: {Utilities.ToString(nnOutput, "f4")}", true);

            // Environment inputs are first of the NN outputs
            double[] environmentInput = Utilities.ArrayCopyOfRange(nnOutput, 0, nnOutput.Length - _turingInputLength);
            
            // Turing inputs are the last of the NN outputs
            double[] turingMachineInput = Utilities.ArrayCopyOfRange(nnOutput, nnOutput.Length - _turingInputLength, _turingInputLength);

            // Activate turing machine with the NN outputs, and store the result for the next iteration, since the environment must be activated in the mean time
            _turingMachineOutput = ProcessTuringMachineOutput(TuringMachine.ProcessInput(turingMachineInput));

            // Return the environment input (remaining NN outputs)
            return environmentInput;
        }

        public void Reset()
        {
            TuringMachine.Reset();
            _turingMachineOutput = ProcessTuringMachineOutput(TuringMachine.GetDefaultRead()); 
        }

        private double[] ProcessTuringMachineOutput(double[][] output)
        {
            return Utilities.Flatten(output);
        }

        public double[] NoveltyVector => TuringMachine.NoveltyVector;
    }
}
