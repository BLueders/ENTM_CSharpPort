using System.Xml;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Threading;
using ENTM.Replay;

namespace ENTM
{
    public abstract class BaseEvaluator<TEnvironment, TController> : IPhenomeEvaluator<IBlackBox> 
        where TEnvironment : IEnvironment
        where TController : IController
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

        private ThreadLocal<TController> _controller;

        public TController Controller
        {
            get
            {
                if (_controller == null)
                {
                    _controller = new ThreadLocal<TController>(NewController);
                }

                return _controller.Value;
            }
        }

        protected abstract TController NewController();

        public abstract void Initialize(XmlElement properties);

        protected ulong _evaluationCount = 0;
        protected bool _stopConditionSatisfied = false;

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            FitnessInfo score = Evaluate(phenome, Iterations, false);

            _evaluationCount++;

            if (score._fitness >= MaxScore) _stopConditionSatisfied = true;

            return score;
        }

        public void Reset()
        {
            Environment.ResetIteration();
            Controller.Reset();
        }

        public abstract FitnessInfo Evaluate(IBlackBox phenome, int iterations, bool record);
        public abstract int Iterations { get; }

        public Recorder Recorder;

        public ulong EvaluationCount => _evaluationCount;
        public bool StopConditionSatisfied => _stopConditionSatisfied;

        public abstract int MaxScore { get; }

        public abstract int EnvironmentInputCount { get; }
        public abstract int EnvironmentOutputCount { get; }
        public abstract int ControllerInputCount { get; }
        public abstract int ControllerOutputCount { get; }
    }
}
