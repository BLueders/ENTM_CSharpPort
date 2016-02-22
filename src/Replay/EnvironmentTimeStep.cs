using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.Replay
{
    public class EnvironmentTimeStep
    {
        public readonly double[] Input;
        public readonly double[] Output;
        public readonly double Score;

        public EnvironmentTimeStep(double[] input, double[] output, double score)
        {
            Input = input;
            Output = output;
            Score = score;
        }
    }
}
