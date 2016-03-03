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
    public class CopyTaskEvaluator : TuringEvaluator<CopyTaskEnvironment>
    {
        private readonly Stopwatch _stopWatch = new Stopwatch();

        private CopyTaskProperties _copyTaskProps;

        public override void Initialize(XmlElement xmlConfig)
        {
            base.Initialize(xmlConfig);
            _copyTaskProps = new CopyTaskProperties(xmlConfig.SelectSingleNode("CopyTaskParams") as XmlElement);
        }

        protected override CopyTaskEnvironment NewEnvironment()
        {
            // This is called from the ThreadLocal Environment, so a new environment is instantiated for each thread
            return new CopyTaskEnvironment(_copyTaskProps);
        }

        public override int MaxScore => 1;
        
        public override int EnvironmentInputCount => _copyTaskProps.VectorSize;

        public override int EnvironmentOutputCount => _copyTaskProps.VectorSize + 2;

        public override int Iterations => _copyTaskProps.Iterations;

        public override double Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            Utility.Debug.LogHeader("STARTING EVAULATION", true);
            double totalScore = 0;
            //int steps = 0;

            //long nnTime = 0;
            //long contTime = 0;
            //long simTime = 0;

            TuringController controller = new TuringController(phenome, TuringMachineProperties);
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
                    //_stopWatch.ResetIteration();

                    // CopyTask can rely on the TM acting first
                    turingMachineOutput = controller.ProcessNNOutputs(Utilities.ArrayCopyOfRange(nnOutput, environmentInputCount, turingMachineInputCount));

                    //contTime += _stopWatch.ElapsedMilliseconds;
                    //_stopWatch.ResetIteration();

                    enviromentOutput = Environment.PerformAction(Utilities.ArrayCopyOfRange(nnOutput, 0, environmentInputCount));

                    //simTime += _stopWatch.ElapsedMilliseconds;

                    //steps++;

                    //_stopWatch.Stop();
                    //_stopWatch.ResetAll();

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
            Environment.ResetIteration();
        }
    }
}
