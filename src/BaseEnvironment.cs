using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Replay;
using ENTM.TuringMachine;
using log4net;

namespace ENTM
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

        public void ResetRandom()
        {
            _sealedRandom = new Random(RandomSeed);
        }

        public Random SealedRandom
        {
            get
            {
                if (_sealedRandom == null)
                    throw new ArgumentNullException("Random object was null, ResetRandom() was not called on this Thread");

                return _sealedRandom;
            }
        } 
    }
}
