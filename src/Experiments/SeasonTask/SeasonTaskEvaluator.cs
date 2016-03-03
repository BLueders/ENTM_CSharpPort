using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Experiments.CopyTask;
using ENTM.Experiments.SeasonTask;
using ENTM.Replay;
using ENTM.TuringMachine;
using ENTM.Utility;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM.Experiments.SeasonTask
{
    class SeasonTaskEvaluator : TuringEvaluator<SeasonTaskEnvironment>
    {
        
        private readonly Stopwatch _stopWatch = new Stopwatch();

        private SeasonTaskProperties _seasonTaskProps;

        protected override SeasonTaskEnvironment NewEnvironment()
        {
            return new SeasonTaskEnvironment(_seasonTaskProps);
        }

        public override void Initialize(XmlElement xmlConfig)
        {
            base.Initialize(xmlConfig);
            _seasonTaskProps = new SeasonTaskProperties(xmlConfig.SelectSingleNode("SeasonTaskParams") as XmlElement);
        }

        public override int MaxScore => 1;

        public override double Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            Utility.Debug.LogHeader("STARTING EVAULATION", true);
            double totalScore = 0;
            TuringController controller = new TuringController(phenome, TuringMachineProperties);
            Environment.Controller = controller;

            int turingMachineInputCount = controller.TuringMachine.InputCount;
            int environmentInputCount = Environment.InputCount;

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
                    double[] nnOutput = controller.ActivateNeuralNetwork(enviromentOutput, turingMachineOutput);

                    // CopyTask can rely on the TM acting first
                    turingMachineOutput = controller.ProcessNNOutputs(Utilities.ArrayCopyOfRange(nnOutput, environmentInputCount, turingMachineInputCount));

                    enviromentOutput = Environment.PerformAction(Utilities.ArrayCopyOfRange(nnOutput, 0, environmentInputCount));

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
        public override int Iterations => _seasonTaskProps.Iterations;


        public override void Reset()
        {
            Environment.ResetIteration();
        }

        public override int EnvironmentInputCount => 1;
        public override int EnvironmentOutputCount => _seasonTaskProps.FoodTypes * _seasonTaskProps.Seasons + 2; // 2 for punishing and reward inputs
    }
}
