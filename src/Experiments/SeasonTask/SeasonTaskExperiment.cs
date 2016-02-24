using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.SeasonTask;

namespace ENTM.Experiments.SeasonsTask
{
    class SeasonTaskExperiment : NeatExperiment<SeasonTaskEvaluator, SeasonTaskEnvironment>
    {
        // The read output from the controller. +1 for bias input!
        public override int InputCount => Evaluator.EnvironmentOutputCount + Evaluator.TuringMachineOutputCount + 1;

        // The input we give the controller in each iteration
        // Will be empty when we expect the controller to read back the sequence.
        // The two extra ones are START and DELIMITER bits.
        public override int OutputCount => Evaluator.EnvironmentInputCount + Evaluator.TuringMachineInputCount;
    }
}
