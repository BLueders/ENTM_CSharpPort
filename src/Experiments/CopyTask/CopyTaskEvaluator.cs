using System;
using System.Diagnostics;
using System.Xml;
using ENTM.Replay;
using ENTM.TuringMachine;
using ENTM.Utility;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskEvaluator : BaseEvaluator<CopyTaskEnvironment, TuringController>
    {
        private readonly Stopwatch _stopWatch = new Stopwatch();

        private TuringMachineProperties _turingMachineProps;
        private CopyTaskProperties _copyTaskProps;

        public override void Initialize(XmlElement xmlConfig)
        {
            _turingMachineProps = new TuringMachineProperties(xmlConfig.SelectSingleNode("TuringMachineParams") as XmlElement);
            _copyTaskProps = new CopyTaskProperties(xmlConfig.SelectSingleNode("CopyTaskParams") as XmlElement);
        }

        protected override CopyTaskEnvironment NewEnvironment()
        {
            // This is called from the ThreadLocal Environment, so a new environment is instantiated for each thread
            return new CopyTaskEnvironment(_copyTaskProps);
        }

        protected override TuringController NewController()
        {
            return new TuringController(_turingMachineProps);
        }

        private int _maxScore = -1;
        public override int MaxScore
        {
            get
            {
                if (_maxScore < 0)
                {
                    _maxScore = (int) (_copyTaskProps.Iterations * _copyTaskProps.MaxSequenceLength * _copyTaskProps.FitnessFactor);
                }
                return _maxScore;
            }
        }

        public int TuringMachineInputCount
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

                return (_turingMachineProps.M + 2 + shifts) * _turingMachineProps.Heads;
            }
        }

        public int TuringMachineOutputCount => _turingMachineProps.M * _turingMachineProps.Heads;

        public int EnvironmentInputCount => _copyTaskProps.VectorSize;

        public int EnvironmentOutputCount => _copyTaskProps.VectorSize + 2;

        public override int Iterations => _copyTaskProps.Iterations;

        public override FitnessInfo Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            if (phenome == null) Console.WriteLine("Warning! Trying to evalutate null phenome!");

            Utility.Debug.LogHeader("STARTING EVAULATION", true);
            double totalScore = 0;

            Controller.BlackBox = phenome;
            Environment.Controller = Controller;

            int turingMachineInputCount = Controller.TuringMachine.InputCount;
            int environmentInputCount = Environment.InputCount;

            AuxFitnessInfo[] noveltyScore = new AuxFitnessInfo[10];

            // For each iteration
            for (int i = 0; i < iterations; i++)
            {
                Utility.Debug.LogHeader($"EVALUATION ITERATION {i}", true);

                Reset();

                double[] turingMachineOutput = Controller.InitialInput;
                double[] enviromentOutput = Environment.InitialObservation;

                if (record)
                {
                    Recorder = new Recorder();
                    Recorder.Start();

                    Controller.TuringMachine.RecordTimeSteps = true;
                    Environment.RecordTimeSteps = true;

                    Recorder.Record(Environment.InitialTimeStep, Controller.TuringMachine.InitialTimeStep);
                }

                while (!Environment.IsTerminated)
                {
                    double[] nnOutput = Controller.ActivateNeuralNetwork(enviromentOutput, turingMachineOutput);

                    //TODO: Move ANN logic into controller class

                    // CopyTask can rely on the TM acting first
                    turingMachineOutput = Controller.ProcessNNOutputs(Utilities.ArrayCopyOfRange(nnOutput, environmentInputCount, turingMachineInputCount));

                    enviromentOutput = Environment.PerformAction(Utilities.ArrayCopyOfRange(nnOutput, 0, environmentInputCount));

                    if (record)
                    {
                        Recorder.Record(Environment.PreviousTimeStep, Controller.TuringMachine.PreviousTimeStep);
                    }
                }

                totalScore += Environment.NormalizedScore;

                Utility.Debug.Log($"EVALUATION Total Score: {totalScore}, Iteration Score: {Environment.CurrentScore}", true);
            }

            // Unregister the phenome
            Controller.BlackBox = null;

            double environmentScore = Math.Max(0d, totalScore / iterations);

            return new FitnessInfo(environmentScore, noveltyScore);
        }

        public override void Reset()
        {
            Environment.Restart();
            Controller.Reset();
        }
    }
}
