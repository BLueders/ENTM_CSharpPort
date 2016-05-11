﻿using System.Xml;
using ENTM.Base;
using ENTM.Replay;
using ENTM.MultiObjective;

namespace ENTM.Experiments.Xor
{
    public class XorEvaluator : BaseEvaluator<XorEnvironment, DefaultController>
    {
        public override void Initialize(XmlElement properties)
        {
            
        }

        public override int Iterations => 1;
        public override int MaxScore => 1;
        public override int EnvironmentInputCount => 1;
        public override int EnvironmentOutputCount => 2;
        public override int ControllerInputCount => 0;
        public override int ControllerOutputCount => 0;
        public override int NoveltyVectorDimensions => 0;
        public override int NoveltyVectorLength => 0;
        public override int MinimumCriteriaLength => 0;

        public override EvaluationInfo Evaluate(DefaultController controller, int iterations, bool record)
        {
            double totalScore = 0;

            for (int i = 0; i < iterations; i++)
            {
                Reset();

                if (record)
                {
                    Recorder = new Recorder();
                    Recorder.Start();

                    Environment.RecordTimeSteps = true;

                    Recorder.Record(Environment.InitialTimeStep);
                }

                double[] enviromentOutput = Environment.InitialObservation;

                while (!Environment.IsTerminated)
                {
                    double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                    enviromentOutput = Environment.PerformAction(environmentInput);

                    if (record)
                    {
                        Recorder.Record(Environment.PreviousTimeStep);
                    }
                }

                totalScore += Environment.NormalizedScore;
            }

            double score = totalScore / iterations;

            return new EvaluationInfo(score);
        }

        protected override void SetupTest()
        {
            Environment.RandomSeed = System.Environment.TickCount;
        }

        protected override void TearDownTest()
        {
            Environment.RandomSeed = 0;
        }

        protected override XorEnvironment NewEnvironment()
        {
            return new XorEnvironment();
        }

        protected override DefaultController NewController()
        {
            return new DefaultController();
        }
    }
}
