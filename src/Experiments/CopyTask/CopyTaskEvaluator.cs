using System.Xml;
using ENTM.TuringMachine;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskEvaluator : TuringEvaluator<CopyTaskEnvironment>
    {
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

        protected override void OnObjectiveEvaluationStart()
        {
            Environment.LengthRule = _copyTaskProps.LengthRule;

            // Reset the environment. This will reset the random
            Environment.ResetAll();
        }

        protected override void OnNoveltyEvaluationStart()
        {
            // We need a fixed sequence length for novelty search
            Environment.LengthRule = LengthRule.Fixed;
          
            // Reset environment so random seed is reset
            Environment.ResetAll();
        }

        protected override void SetupTest()
        {
            Environment.LengthRule = LengthRule.Fixed;
            Environment.RandomSeed = System.Environment.TickCount;
        }

        protected override void SetupGeneralizationTest()
        {
            SetupTest();
            Environment.MaxSequenceLength = 100;
        }

        protected override void TearDownTest()
        {
            Environment.LengthRule = _copyTaskProps.LengthRule;
            Environment.RandomSeed = _copyTaskProps.RandomSeed;
            Environment.MaxSequenceLength = _copyTaskProps.MaxSequenceLength;
        }

        public override int MaxScore => 1;

        public override int EnvironmentInputCount => _copyTaskProps.VectorSize;

        // +2 for start and delimiter bits
        public override int EnvironmentOutputCount => _copyTaskProps.VectorSize + 2;

        public override int Iterations => _copyTaskProps.Iterations;
    }
}
