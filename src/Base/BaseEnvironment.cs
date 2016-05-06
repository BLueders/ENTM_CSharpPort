using System;
using ENTM.Replay;
using log4net;

namespace ENTM.Base
{
    public abstract class BaseEnvironment : IEnvironment
    {
        protected static readonly ILog _logger = LogManager.GetLogger(typeof(BaseEnvironment));

        private Random _sealedRandom;

        public abstract bool RecordTimeSteps { get; set; }
        public abstract EnvironmentTimeStep InitialTimeStep { get; }
        public abstract EnvironmentTimeStep PreviousTimeStep { get; }
        public abstract IController Controller { get; set; }
        public abstract int InputCount { get; }
        public abstract int OutputCount { get; }
        public abstract double[] InitialObservation { get; }
        public abstract double CurrentScore { get; }
        public abstract double MaxScore { get; }
        public abstract double NormalizedScore { get; }
        public abstract bool IsTerminated { get; }
        public abstract int RandomSeed { get; set; }

        public abstract void ResetAll();
        public abstract void ResetIteration();
        public abstract double[] PerformAction(double[] action);
        public abstract int TotalTimeSteps { get; }
        public abstract int MaxTimeSteps { get; }

        public void ResetRandom()
        {
            _sealedRandom = new Random(RandomSeed);
        }

        public Random SealedRandom
        {
            get
            {
                if (_sealedRandom == null)
                    ResetRandom();

                return _sealedRandom;
            }
        } 
    }
}
