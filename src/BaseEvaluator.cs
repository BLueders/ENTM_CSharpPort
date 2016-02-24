using System.Xml;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Threading;
using ENTM.Replay;

namespace ENTM
{
    public abstract class BaseEvaluator<TEnvironment> : IPhenomeEvaluator<IBlackBox> where TEnvironment : IEnvironment
    {
        
        private ThreadLocal<TEnvironment> _environment;

        /// <summary>
        /// ThreadLocal environment instance
        /// </summary>
        public TEnvironment Environment
        {
            get
            {
                if (_environment == null)
                {
                    _environment = new ThreadLocal<TEnvironment>(NewEnvironment);
                }

                return _environment.Value;
            }
        }

        /// <summary>
        /// Override must return a new instantiated environment, which will be used for the ThreadLocal environment
        /// </summary>
        /// <returns>a new instance of TEnvironment</returns>
        protected abstract TEnvironment NewEnvironment();

        public abstract void Initialize(XmlElement properties);

        public abstract int MaxScore { get; }

        protected ulong _evaluationCount = 0;
        protected bool _stopConditionSatisfied = false;

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            double score = Evaluate(phenome, Iterations, false);

            _evaluationCount++;

            if (score >= MaxScore) _stopConditionSatisfied = true;

            return new FitnessInfo(score, 0);
        }

        public abstract double Evaluate(IBlackBox phenome, int iterations, bool record);
        public abstract int Iterations { get; }
        public abstract void Reset();

        public Recorder Recorder;

        public ulong EvaluationCount => _evaluationCount;
        public bool StopConditionSatisfied => _stopConditionSatisfied;
    }
}
