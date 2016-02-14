using System;
using System.Diagnostics;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Xml;
using ENTM.TuringMachine;
using SharpNeat.Core;
using SharpNeat.Domains;
using SharpNeat.Phenomes;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskEvaluator : BaseEvaluator<CopyTaskEnvironment>
    {
        private const int DEFAULT_ITERATIONS = 10;

        private readonly Stopwatch _stopWatch = new Stopwatch();

        private int _iterations;
        private ENTMProperties _properties;

        public override void Initialize(XmlElement xmlConfig)
        {
            _iterations = XmlUtils.TryGetValueAsInt(xmlConfig, "Iterations") ?? DEFAULT_ITERATIONS;
            _properties = new ENTMProperties(xmlConfig);
            Environment = new CopyTaskEnvironment(_properties);
        }

        public override int MaxScore => Environment.MaxScore * _iterations;

        public int TuringMachineInputCount
        {
            get
            {
                int shifts = 0;
                switch (_properties.ShiftMode)
                {
                    case ShiftMode.Multiple:
                        shifts = _properties.ShiftLength;
                        break;
                    case ShiftMode.Single:
                        shifts = 1;
                        break;
                }

                return (_properties.M + 2 + shifts) * _properties.Heads;
            }
        }

        public int TuringMachineOutputCount => _properties.M * _properties.Heads;

        public int EnvironmentInputCount => Environment.InputCount;

        public int EnvironmentOutputCount => Environment.OutputCount;

        public override FitnessInfo Evaluate(IBlackBox phenome)
        {
            double totalScore = 0;
            //Environment.Reset();
            int steps = 0;

            long nnTime = 0;
            long contTime = 0;
            long simTime = 0;

            TuringController controller = new TuringController(phenome, _properties);

            int turingMachineInputCount = controller.TuringMachine.InputCount;
            int environmentInputCount = Environment.InputCount;

            // For each iteration
            for (int i = 0; i < _iterations; i++)
            {
                Reset();
                Environment.Restart();

                double[] turingMachineOutput = controller.InitialInput;
                double[] enviromentOutput = Environment.InitialObservation;

                while (!Environment.IsTerminated)
                {
                    _stopWatch.Start();

                    double[] nnOutput = controller.ActivateNeuralNetwork(enviromentOutput, turingMachineOutput);

                    nnTime += _stopWatch.ElapsedMilliseconds;
                    _stopWatch.Restart();

                    // CopyTask can rely on the TM acting first
                    turingMachineOutput = controller.ProcessNNOutputs(Utilities.ArrayCopyOfRange(nnOutput, environmentInputCount, turingMachineInputCount));

                    contTime += _stopWatch.ElapsedMilliseconds;
                    _stopWatch.Restart();

                    enviromentOutput = Environment.PerformAction(Utilities.ArrayCopyOfRange(nnOutput, 0, environmentInputCount));

                    simTime += _stopWatch.ElapsedMilliseconds;

                    steps++;

                    _stopWatch.Stop();
                    _stopWatch.Reset();
                }

                totalScore += Environment.CurrentScore;
            }

            double result = Math.Max(0.0, totalScore);

            return new FitnessInfo(result, result);
        }

        public override void Reset()
        {
            Environment.Reset();
        }
    }
}
