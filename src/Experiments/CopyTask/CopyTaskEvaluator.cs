using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using ENTM.Replay;
using ENTM.TuringMachine;
using ENTM.Utility;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using SharpNeat.Domains;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskEvaluator : BaseEvaluator<CopyTaskEnvironment>
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

        public override double Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            Utility.Debug.LogHeader("STARTING EVAULATION", true);
            double totalScore = 0;
            //int steps = 0;

            //long nnTime = 0;
            //long contTime = 0;
            //long simTime = 0;

            TuringController controller = new TuringController(phenome, _turingMachineProps);
            Environment.Controller = controller;

            int turingMachineInputCount = controller.TuringMachine.InputCount;
            int environmentInputCount = Environment.InputCount;

            // For each iteration
            for (int i = 0; i < iterations; i++)
            {
                Utility.Debug.LogHeader($"EVALUATION ITERATION {i}", true);

                Reset();
                controller.Reset();

                double[] turingMachineOutput = controller.InitialInput;
                double[] enviromentOutput = Environment.InitialObservation;

                if (record)
                {
                    Recorder = new Recorder();
                    Recorder.Start();

                    controller.TuringMachine.RecordTimeSteps = true;
                    Environment.RecordTimeSteps = true;

                    Recorder.Record(Environment.InitialTimeStep, controller.TuringMachine.InitialTimeStep);
                }

                while (!Environment.IsTerminated)
                {
                    //_stopWatch.Start();

                    double[] nnOutput = controller.ActivateNeuralNetwork(enviromentOutput, turingMachineOutput);

                    //nnTime += _stopWatch.ElapsedMilliseconds;
                    //_stopWatch.Restart();

                    // CopyTask can rely on the TM acting first
                    turingMachineOutput = controller.ProcessNNOutputs(Utilities.ArrayCopyOfRange(nnOutput, environmentInputCount, turingMachineInputCount));

                    //contTime += _stopWatch.ElapsedMilliseconds;
                    //_stopWatch.Restart();

                    enviromentOutput = Environment.PerformAction(Utilities.ArrayCopyOfRange(nnOutput, 0, environmentInputCount));

                    //simTime += _stopWatch.ElapsedMilliseconds;

                    //steps++;

                    //_stopWatch.Stop();
                    //_stopWatch.Reset();

                    if (record)
                    {
                        Recorder.Record(Environment.PreviousTimeStep, controller.TuringMachine.PreviousTimeStep);
                    }
                }

                totalScore += Environment.NormalizedScore;

                Utility.Debug.Log($"EVALUATION Total Score: {totalScore}, Iteration Score: {Environment.CurrentScore}", true);
            }


            return Math.Max(0d, totalScore / iterations);
        }

        public override void Reset()
        {
            Environment.Restart();
        }
    }
}
