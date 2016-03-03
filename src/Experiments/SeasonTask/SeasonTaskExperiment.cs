using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.SeasonTask;

namespace ENTM.Experiments.SeasonTask
{
    class SeasonTaskExperiment : NeatExperiment<SeasonTaskEvaluator, SeasonTaskEnvironment>
    {
        // The read output from the controller. +1 for bias input!
        public override int InputCount => _evaluator.EnvironmentOutputCount + _evaluator.TuringMachineOutputCount + 1;

        // The input we give the controller in each iteration
        // Will be empty when we expect the controller to read back the sequence.
        // The two extra ones are START and DELIMITER bits.
        public override int OutputCount => _evaluator.EnvironmentInputCount + _evaluator.TuringMachineInputCount;
    }
}
