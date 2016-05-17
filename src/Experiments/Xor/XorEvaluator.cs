using System.Xml;
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
        public override int NoveltyVectorDimensions { get; }
        public override int NoveltyVectorLength { get; }
        public override int MinimumCriteriaLength => 0;

        protected override void EvaluateObjective(DefaultController controller, int iterations, ref EvaluationInfo evaluation)
        {
            double totalScore = 0;

            for (int i = 0; i < iterations; i++)
            {
                Reset();

                double[] enviromentOutput = Environment.InitialObservation;

                while (!Environment.IsTerminated)
                {
                    double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                    enviromentOutput = Environment.PerformAction(environmentInput);
                }

                totalScore += Environment.NormalizedScore;
            }

            evaluation.ObjectiveFitness = totalScore / iterations;
        }

        protected override void EvaluateNovelty(DefaultController controller, ref EvaluationInfo evaluation)
        {
            throw new System.NotImplementedException();
        }

        protected override void EvaluateRecord(DefaultController controller, int iterations, ref EvaluationInfo evaluation)
        {
            Reset();

            Recorder = new Recorder();
            Recorder.Start();

            Environment.RecordTimeSteps = true;

            Recorder.Record(Environment.InitialTimeStep);

            double[] enviromentOutput = Environment.InitialObservation;

            while (!Environment.IsTerminated)
            {
                double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                enviromentOutput = Environment.PerformAction(environmentInput);

                Recorder.Record(Environment.PreviousTimeStep);
            }

            evaluation.ObjectiveFitness = Environment.NormalizedScore;
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
