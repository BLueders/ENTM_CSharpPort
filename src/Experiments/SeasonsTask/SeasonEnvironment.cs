using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Replay;

namespace ENTM.Experiments.SeasonsTask
{
    class SeasonEnvironment : IEnvironment
    {
        public bool RecordTimeSteps { get; set; }
        public EnvironmentTimeStep InitialTimeStep { get; }
        public EnvironmentTimeStep PreviousTimeStep { get; }
        public IController Controller { get; set; }
        public int InputCount { get; }
        public int OutputCount { get; }
        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public double[] InitialObservation { get; }
        public double[] PerformAction(double[] action)
        {
            throw new NotImplementedException();
        }

        public double CurrentScore { get; }
        public double MaxScore { get; }
        public double NormalizedScore { get; }
        public bool IsTerminated { get; }
        public int TotalTimeSteps { get; }
    }
}
