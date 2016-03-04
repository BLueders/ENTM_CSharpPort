using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Replay;

namespace ENTM
{
    public abstract class BaseEnvironment : IEnvironment
    {
        public abstract bool RecordTimeSteps { get; set; }
        public abstract EnvironmentTimeStep InitialTimeStep { get; }
        public abstract EnvironmentTimeStep PreviousTimeStep { get; }
        public abstract IController Controller { get; set; }
        public abstract int InputCount { get; }
        public abstract int OutputCount { get; }
        public abstract void ResetAll();

        public virtual void ResetIteration()
        {
            _sealedRandom = new Random(RandomSeed);
        }

        public abstract double[] InitialObservation { get; }
        public abstract double[] PerformAction(double[] action);
        public abstract double CurrentScore { get; }
        public abstract double MaxScore { get; }
        public abstract double NormalizedScore { get; }
        public abstract bool IsTerminated { get; }
        public abstract int TotalTimeSteps { get; }

        public abstract int RandomSeed { get; set; }

        private Random _sealedRandom;
        public Random SealedRandom => _sealedRandom;
    }
}
