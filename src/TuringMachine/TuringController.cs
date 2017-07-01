using ENTM.Base;
using ENTM.NoveltySearch;
using ENTM.Utility;
using SharpNeat.Phenomes;

namespace ENTM.TuringMachine
{
    public class TuringController : IController
    {
        public IBlackBox Phenome { get; set; }

        public ITuringMachine TuringMachine { get; }

        public NoveltySearchInfo NoveltySearch
        {
            get { return TuringMachine.NoveltySearch; }
            set { TuringMachine.NoveltySearch = value; }
        }

        public TuringController(TuringMachineProperties props)
        {
            TuringMachine = new MinimalTuringMachine(props);
            _turingInputLength = TuringMachine.InputCount;
        }

        private readonly int _turingInputLength;
        private double[] _turingMachineOutput;

        public double[] ActivateNeuralNetwork(double[] environmentOutput)
        {
            Debug.DLog($"Neural Network Input:  {Utilities.ToString(environmentOutput, "f4")}{Utilities.ToString(_turingMachineOutput, "f4")}", true);

            // Reset the neural network state
            Phenome.ResetState();
       
            // NN Input is the output from the environment, and the output from the turing machine in the previous activation
            Phenome.InputSignalArray.CopyFrom(environmentOutput, 0);
            Phenome.InputSignalArray.CopyFrom(_turingMachineOutput, environmentOutput.Length);
            
            // Activate the neural network
            Phenome.Activate();

            double[] nnOutput = new double[Phenome.OutputSignalArray.Length];
            Phenome.OutputSignalArray.CopyTo(nnOutput, 0);

            Debug.DLog($"Neural Network Output: {Utilities.ToString(nnOutput, "f4")}", true);

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

            _turingMachineOutput = ProcessTuringMachineOutput(TuringMachine.DefaultRead); 
        }

        private double[] ProcessTuringMachineOutput(double[][] output)
        {
            return Utilities.Flatten(output);
        }
    }
}
