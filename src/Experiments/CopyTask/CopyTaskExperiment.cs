using System;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Domains;
using SharpNeat.Phenomes;

namespace ENTM.Experiments
{
    class CopyTaskExperiment : NeatExperiment<CopyTaskEnvironment>
    {
        private readonly CopyTaskEnvironment _environment;

        public CopyTaskExperiment()
        {
            _environment = new CopyTaskEnvironment();
        }

        public override IPhenomeEvaluator<IBlackBox> PhenomeEvaluator { get; }

        // The read output from the controller
        public override int InputCount => Environment.InputCount;

        // The input we give the controller in each iteration
        // Will be empty when we expect the controller to read back the sequence.
        // The two extra ones are START and DELIMITER bits.
        public override int OutputCount => Environment.OutputCount;

        public override CopyTaskEnvironment Environment => _environment;
    }
}
