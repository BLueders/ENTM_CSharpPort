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
            return new SeasonTaskEnvironment(_seasonTaskProps);
        }

        public override void Initialize(XmlElement xmlConfig)
        {
            base.Initialize(xmlConfig);
            _seasonTaskProps = new SeasonTaskProperties(xmlConfig.SelectSingleNode("SeasonTaskParams") as XmlElement);
        }

        public override int MaxScore => 1;

        // Eat or don't eat
        public override int EnvironmentInputCount => 1;

        // + 2 for punishing and reward inputs
        public override int EnvironmentOutputCount => _seasonTaskProps.FoodTypes * _seasonTaskProps.Seasons + 2; 

        public override int Iterations => _seasonTaskProps.Iterations;
    }
}
