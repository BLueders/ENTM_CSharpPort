using System;
using System.Diagnostics;
using System.Xml;
using ENTM.NoveltySearch;
using ENTM.Replay;
using ENTM.TuringMachine;
using ENTM.Utility;
using SharpNeat.Core;
using SharpNeat.Phenomes;

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

        protected override void OnEvaluationStart()
        {
            // We need a fixed sequence length for novelty search
            Environment.LengthRule = NoveltySearchEnabled ? LengthRule.Fixed : _copyTaskProps.LengthRule;
        }

        protected override void SetupTest()
        {
            Environment.LengthRule = LengthRule.Fixed;
            Environment.RandomSeed = System.Environment.TickCount;
        }

        protected override void TearDownTest()
        {
            Environment.LengthRule = _copyTaskProps.LengthRule;
            Environment.MaxSequenceLength = _copyTaskProps.MaxSequenceLength;
            Environment.RandomSeed = _copyTaskProps.RandomSeed;
        }

        public override int MaxScore => 1;

        public override int EnvironmentInputCount => _copyTaskProps.VectorSize;

        // +2 for start and delimiter bits
        public override int EnvironmentOutputCount => _copyTaskProps.VectorSize + 2;

        public override int Iterations => _copyTaskProps.Iterations;

        public override int NoveltyVectorLength
        {
            get
            {
                switch (NoveltySearchParameters.NoveltyVectorMode)
                {
                    case NoveltyVector.WritePattern:
                        return _copyTaskProps.MaxSequenceLength*2 + 2 + 1;

                    case NoveltyVector.ReadContent:

                        // total timesteps * M + 1 for minimum criteria
                        return (_copyTaskProps.MaxSequenceLength*2 + 2)*_turingMachineProps.M + 1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
