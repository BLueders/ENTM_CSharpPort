using System;
using System.Xml;
using ENTM.Base;
using ENTM.NoveltySearch;
using ENTM.Replay;
using ENTM.MultiObjective;

namespace ENTM.TuringMachine
{
    public abstract class TuringEvaluator<TEnvironment> : BaseEvaluator<TEnvironment, TuringController> where TEnvironment : IEnvironment
    {
        protected TuringMachineProperties _turingMachineProps;

        public override void Initialize(XmlElement xmlConfig)
        {
            _turingMachineProps = new TuringMachineProperties(xmlConfig.SelectSingleNode("TuringMachineParams") as XmlElement);
        }

        protected override TuringController NewController()
        {
            return new TuringController(_turingMachineProps);
        }

        public override int ControllerInputCount
        {
            get
            {
                int shifts = 0;
                switch (_turingMachineProps.ShiftMode)
                {
                    case ShiftMode.Multiple:
                        shifts = _turingMachineProps.ShiftLength;
                        break;
                    case ShiftMode.Single:
                        shifts = 1;
                        break;
                }
                
                // Write key size (M) + 2 (write interp and content jump) + shifts - for each head (probably only one)
                return (_turingMachineProps.M + 2 + shifts) * _turingMachineProps.Heads;
            }
        }

        // Read key for the turing machine for each head
        public override int ControllerOutputCount => _turingMachineProps.M * _turingMachineProps.Heads;

        protected override void EvaluateObjective(TuringController controller, int iterations, ref EvaluationInfo evaluation)
        {
            Utility.Debug.DLogHeader("STARTING EVAULATION", true);

            double totalScore = 0;

            // Iteration loop
            for (int i = 0; i < iterations; i++)
            {
                Utility.Debug.DLogHeader($"EVALUATION ITERATION {i}", true);

                Reset();

                double[] enviromentOutput = Environment.InitialObservation;

                // Environment loop
                while (!Environment.IsTerminated)
                {
                    // Activate the controller with the environment output. 
                    // The turing controller will handle the turing machine I/O
                    double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                    // Activate the environment with the output from the controller (NN)
                    enviromentOutput = Environment.PerformAction(environmentInput);
                }

                totalScore += Environment.NormalizedScore;

                Utility.Debug.DLog($"EVALUATION Total Score: {totalScore}, Iteration Score: {Environment.CurrentScore}", true);
            }

            // Calculate the total normalized score (0-1)
            evaluation.ObjectiveFitness = Math.Max(0d, totalScore / iterations);
        }

        protected override void EvaluateNovelty(TuringController controller, ref EvaluationInfo evaluation)
        {
            Reset();
            double[] enviromentOutput = Environment.InitialObservation;

            // Environment loop
            while (!Environment.IsTerminated)
            {
                // Activate the controller with the environment output. 
                // The turing controller will handle the turing machine I/O
                double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                // Activate the environment with the output from the controller (NN)
                enviromentOutput = Environment.PerformAction(environmentInput);
            }

            NoveltySearchInfo result = controller.NoveltySearch;

            evaluation.NoveltyVectors = result.NoveltyVectors;
            evaluation.MinimumCriteria = result.MinimumCriteria;
        }

        protected override void EvaluateRecord(TuringController controller, ref EvaluationInfo evaluation)
        {
            Reset();

            Recorder = new Recorder();
            Recorder.Start();

            controller.TuringMachine.RecordTimeSteps = true;
            Environment.RecordTimeSteps = true;

            Recorder.Record(Environment.InitialTimeStep, controller.TuringMachine.InitialTimeStep);

            double[] enviromentOutput = Environment.InitialObservation;

            // Environment loop
            while (!Environment.IsTerminated)
            {
                // Activate the controller with the environment output. 
                // The turing controller will handle the turing machine I/O
                double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                // Activate the environment with the output from the controller (NN)
                enviromentOutput = Environment.PerformAction(environmentInput);

                Recorder.Record(Environment.PreviousTimeStep, controller.TuringMachine.PreviousTimeStep);
            }

            Recorder.FinalTuringTape = Controller.TuringMachine.TapeValues;

            evaluation.ObjectiveFitness = Environment.NormalizedScore;
        }

        public override int NoveltyVectorDimensions
        {
            get
            {
                switch (NoveltySearchParameters.VectorMode)
                {
                    case NoveltyVectorMode.WritePattern:
                        return 1;

                    case NoveltyVectorMode.ReadContent:
                        return _turingMachineProps.M;

                    case NoveltyVectorMode.WritePatternAndInterp:
                        return 2;

                    case NoveltyVectorMode.ShiftJumpInterp:
                        return 3;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown novelty vector mode" + NoveltySearchParameters.VectorMode);
                }
            }
        }

        // total timesteps - 1 (initial timestep is not scored)
        public override int NoveltyVectorLength => Environment.MaxTimeSteps - 1;

        // Minimum criteria: redundant timesteps + total timesteps
        public override int MinimumCriteriaLength => 2;
    }
}