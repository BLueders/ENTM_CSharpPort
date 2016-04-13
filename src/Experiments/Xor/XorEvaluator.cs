using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Replay;
using SharpNeat.Core;

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
        public override int NoveltyVectorLength { get; }

        public override FitnessInfo Evaluate(DefaultController controller, int iterations, bool record)
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

            return new FitnessInfo(score, score);
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
