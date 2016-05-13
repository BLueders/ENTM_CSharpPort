using System.Xml;
using ENTM.TuringMachine;

namespace ENTM.Experiments.SeasonTask
{
    class SeasonTaskEvaluator : TuringEvaluator<SeasonTaskEnvironment>
    {
        private SeasonTaskProperties _seasonTaskProps;

        protected override SeasonTaskEnvironment NewEnvironment()
        {
            return new MultiStepSeasonTaskEnviroment(_seasonTaskProps, _seasonTaskProps.StepsPerFood);
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

        protected override void SetupGeneralizationTest()
        {
            base.SetupGeneralizationTest();
            Environment.DaysMin = 4;
            Environment.DaysMax = 10;
            Environment.Years = 20;
        }

        protected override void TearDownTest()
        {
            Environment.RandomSeed = _seasonTaskProps.RandomSeed;
            Environment.DaysMin = _seasonTaskProps.DaysMin;
            Environment.DaysMax = _seasonTaskProps.DaysMax;
            Environment.Years = _seasonTaskProps.Years;
        }
                
        public override int MaxScore => 1;

        // Eat or don't eat
        public override int EnvironmentInputCount => 1;

        // + 2 for punishing and reward inputs
        public override int EnvironmentOutputCount => _seasonTaskProps.FoodTypes * _seasonTaskProps.Seasons + 2;

        public override int Iterations => _seasonTaskProps.Iterations;
    }
}
