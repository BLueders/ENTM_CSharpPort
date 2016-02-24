using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Experiments.CopyTask;
using ENTM.Experiments.SeasonsTask;
using ENTM.TuringMachine;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM.Experiments.SeasonTask
{
    class SeasonTaskEvaluator : BaseEvaluator<SeasonTaskEnvironment>
    {

        private readonly Stopwatch _stopWatch = new Stopwatch();

        private TuringMachineProperties _turingMachineProps;
        private SeasonTaskProperties _seasonTaskProps;

        protected override SeasonTaskEnvironment NewEnvironment()
        {
            return new SeasonTaskEnvironment(_seasonTaskProps);
        }

        public override void Initialize(XmlElement properties)
        {
            _turingMachineProps = new TuringMachineProperties(properties.SelectSingleNode("TuringMachineParams") as XmlElement);
            _seasonTaskProps = new SeasonTaskProperties(properties.SelectSingleNode("SeasonTaskParams") as XmlElement);
        }

        public override int MaxScore { get; }
        public override FitnessInfo Evaluate(IBlackBox phenome)
        {
            throw new NotImplementedException();
        }

        public override void Reset()
        {
            throw new NotImplementedException();
        }
    }

}
