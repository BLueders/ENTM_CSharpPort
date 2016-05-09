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

        public override EvaluationInfo Evaluate(TuringController controller, int iterations, bool record)
        {
            if (controller == null) throw new ArgumentNullException("Controller was null");

            Utility.Debug.DLogHeader("STARTING EVAULATION", true);

            double totalScore = 0;

            NoveltySearchInfo[] noveltySearch = null;

            if (NoveltySearchEnabled)
            {
                noveltySearch = new NoveltySearchInfo[iterations];
            }

            // Iteration loop
            for (int i = 0; i < iterations; i++)
            {
                Utility.Debug.DLogHeader($"EVALUATION ITERATION {i}", true);

                Reset();

                double[] enviromentOutput = Environment.InitialObservation;

                if (record)
                {
                    Recorder = new Recorder();
                    Recorder.Start();

                    controller.TuringMachine.RecordTimeSteps = true;
                    Environment.RecordTimeSteps = true;

                    Recorder.Record(Environment.InitialTimeStep, controller.TuringMachine.InitialTimeStep);
                }

                // Environment loop
                while (!Environment.IsTerminated)
                {
                    // Activate the controller with the environment output. 
                    // The turing controller will handle the turing machine I/O
                    double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                    // Activate the environment with the output from the controller (NN)
                    enviromentOutput = Environment.PerformAction(environmentInput);

                    if (record)
                    {
                        Recorder.Record(Environment.PreviousTimeStep, controller.TuringMachine.PreviousTimeStep);
                    }
                }

                if (record)
                {
                    Recorder.FinalTuringTape = Controller.TuringMachine.TapeValues;
                }

                totalScore += Environment.NormalizedScore;

                if (NoveltySearchEnabled)
                {
                    noveltySearch[i] = controller.NoveltySearch;
                }

                Utility.Debug.DLog($"EVALUATION Total Score: {totalScore}, Iteration Score: {Environment.CurrentScore}", true);
            }

            // Calculate the total normalized score (0-1)
            double objectiveFitness = Math.Max(0d, totalScore / iterations);

            double[][] noveltyVectorAverages = null;
            double[] minimumCriteriaAverages = null;

            if (NoveltySearchEnabled)
            {
                // Calculate average novelty vectors and minimum criteria
                double[][] noveltyVectorTotals = new double[NoveltyVectorLength][];
                for (int i = 0; i < NoveltyVectorLength; i++)
                {
                    noveltyVectorTotals[i] = new double[NoveltyVectorDimensions];
                }

                double[] minimumCriteriaTotals = new double[MinimumCriteriaLength];

                for (int i = 0; i < iterations; i++)
                {
                    for (int j = 0; j < NoveltyVectorLength; j++)
                    {
                        for (int k = 0; k < NoveltyVectorDimensions; k++)
                        {
                            noveltyVectorTotals[j][k] += noveltySearch[i].NoveltyVectors[j][k];
                        }
                    }

                    for (int j = 0; j < MinimumCriteriaLength; j++)
                    {
                        minimumCriteriaTotals[j] += noveltySearch[i].MinimumCriteria[j];
                    }
                }

                noveltyVectorAverages = new double[NoveltyVectorLength][];
                for (int i = 0; i < NoveltyVectorLength; i++)
                {
                    noveltyVectorAverages[i] = new double[NoveltyVectorDimensions];
                }

                minimumCriteriaAverages = new double[MinimumCriteriaLength];

    
                for (int i = 0; i < NoveltyVectorLength; i++)
                {
                    for (int j = 0; j < NoveltyVectorDimensions; j++)
                    {
                        noveltyVectorAverages[i][j] = noveltyVectorTotals[i][j] / iterations;
                    }
                }

                for (int i = 0; i < MinimumCriteriaLength; i++)
                {
                    minimumCriteriaAverages[i] = minimumCriteriaTotals[i] / iterations;
                }
            }

            return new EvaluationInfo(objectiveFitness, noveltyVectorAverages, minimumCriteriaAverages);
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