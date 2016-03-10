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
        private SeasonTaskProperties _seasonTaskProps;

        protected override SeasonTaskEnvironment NewEnvironment()
        {
            return new OneStepSeasonTaskEnviroment(_seasonTaskProps);
        }

        public override void Initialize(XmlElement xmlConfig)
        {
            base.Initialize(xmlConfig);
            _seasonTaskProps = new SeasonTaskProperties(xmlConfig.SelectSingleNode("SeasonTaskParams") as XmlElement);
        }

        protected override void SetupTest()
        {
            Environment.RandomSeed = System.Environment.TickCount;
        }

        protected override void TearDownTest()
        {
            Environment.RandomSeed = _seasonTaskProps.RandomSeed;
        }

        public override int MaxScore => 1;

        // Eat or don't eat
        public override int EnvironmentInputCount => 1;

        // + 2 for punishing and reward inputs
        public override int EnvironmentOutputCount => _seasonTaskProps.FoodTypes * _seasonTaskProps.Seasons + 2;

        // TODO: Novelty vector length - should be total number of timesteps in environment
        public override int NoveltyVectorLength => 0;

        public override int Iterations => _seasonTaskProps.Iterations;
    }
}
