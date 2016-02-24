using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Replay;
using SharpNeat.Domains;
using SharpNeat.Phenomes;

namespace ENTM.Experiments
{
    interface IExperiment : INeatExperiment
    {
        Recorder Recorder { get; }

        double Evaluate(IBlackBox phenome, int iterations, bool record);
    }
}
