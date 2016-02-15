using System.Xml;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Threading;

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
                    _environment = new ThreadLocal<TEnvironment>(() =>
                    {
                        return NewEnvironment();
                    });
                }

                return _environment.Value;
            }
        }

        /// <summary>
        /// Override must return a new instantiated method, which will be used for the ThreadLocal environment
        /// </summary>
        /// <returns>a new instance of TEnvironment</returns>
        protected abstract TEnvironment NewEnvironment();

        public abstract void Initialize(XmlElement properties);

        public abstract int MaxScore { get; }

        protected ulong _evaluationCount = 0;
        protected bool _stopConditionSatisfied = false;

        public abstract FitnessInfo Evaluate(IBlackBox phenome);

        public abstract void Reset();


        public ulong EvaluationCount => _evaluationCount;
        public bool StopConditionSatisfied => _stopConditionSatisfied;
    }
}
