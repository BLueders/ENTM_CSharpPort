using System;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Threading;
using ENTM.NoveltySearch;
using ENTM.Replay;

namespace ENTM
{
    public abstract class BaseEvaluator<TEnvironment, TController> : IPhenomeEvaluator<IBlackBox>
        where TEnvironment : IEnvironment
        where TController : IController
    {
        protected ulong _evaluationCount = 0;
        public ulong EvaluationCount => _evaluationCount;

        protected bool _stopConditionSatisfied = false;
        public bool StopConditionSatisfied => _stopConditionSatisfied;

        public bool NoveltySearchEnabled { get; set; }

        public NoveltySearchParameters NoveltySearchParameters { get; set; }

        public Recorder Recorder;

        public abstract void Initialize(XmlElement properties);

        /// <summary>
        /// Override this if we need to do something at the beginning of a new evaluation
        /// </summary>
        protected virtual void OnEvaluationStart()
        {
            
        }

        /// <summary>
        /// If we need to do some initialization, like change the parameters before testing a phenome, we can override this and do it here
        /// </summary>
        protected virtual void SetupTest()
        {

        }

        /// <summary>
        /// Return the evaluator to its original state after a test
        /// </summary>
        protected virtual void TearDownTest()
        {

        }

        public abstract int Iterations { get; }
        public abstract int MaxScore { get; }

        public abstract int EnvironmentInputCount { get; }
        public abstract int EnvironmentOutputCount { get; }
        public abstract int ControllerInputCount { get; }
        public abstract int ControllerOutputCount { get; }

        public abstract int NoveltyVectorLength { get; }

        public abstract FitnessInfo Evaluate(TController controller, int iterations, bool record);

        private static readonly object _lockEnvironment = new object();
        private static readonly object _lockController = new object();

        private ThreadLocal<TEnvironment> _environment;

        /// <summary>
        /// ThreadLocal environment instance
        /// </summary>
        protected TEnvironment Environment
        {
            get
            {
                lock (_lockEnvironment)
                {
                    if (_environment == null)
                    {
                        _environment = new ThreadLocal<TEnvironment>(NewEnvironment);
                    }
                    return _environment.Value;
                }
            }
        }

        /// <summary>
        /// Override must return a new environment instance, which will be used for the ThreadLocal environment
        /// </summary>
        /// <returns>a new instance of TEnvironment</returns>
        protected abstract TEnvironment NewEnvironment();

        private ThreadLocal<TController> _controller;

        protected TController Controller
        {
            get
            {
                lock (_lockController)
                {
                    if (_controller == null)
                    {
                        _controller = new ThreadLocal<TController>(NewController);
                    }
                    return _controller.Value;
                }
            }
        }

        /// <summary>
        /// Override must return a new controller instance, which will be used for all evaluations on the current thread
        /// </summary>
        /// <returns>a new instance of TEnvironment</returns>
        protected abstract TController NewController();


        /// <summary>
        /// Main evaluation loop. Called from EA. Runs on separate / multiple threads.
        /// </summary>
        /// <param name="phenome"></param>
        /// <returns></returns>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            OnEvaluationStart();

            FitnessInfo score = Evaluate(phenome, Iterations, false);

            _evaluationCount++;

            if (score._fitness >= MaxScore * .99f)
            {
                _stopConditionSatisfied = true;
            }

            return score;
        }

        /// <summary>
        /// Evaluate a single phenome only once. Use this to test a champion. Runs on the main thread.
        /// </summary>
        /// <param name="phenome">The phenome to be tested</param>
        /// <param name="iterations">Number of evaluations</param>
        /// <param name="record">Determines if the evaluation should be recorded</param>
        /// <returns></returns>
        public FitnessInfo TestPhenome(IBlackBox phenome, int iterations)
        {
            SetupTest();

            FitnessInfo score = Evaluate(phenome, iterations, true);

            TearDownTest();

            return score;
        }

        private FitnessInfo Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            // Register the phenome
            Controller.Phenome = phenome;

            Controller.ScoreNovelty = NoveltySearchEnabled;
            Controller.NoveltyVectorLength = NoveltyVectorLength;
            Controller.NoveltyVectorMode = NoveltySearchParameters.NoveltyVectorMode;

            Environment.ResetAll();
            Environment.Controller = Controller;

            // Evaluate the controller / phenome
            FitnessInfo score = Evaluate(Controller, iterations, record);

            // Unregister the phenome
            Controller.Phenome = null;

            return score;
        }

        public void Reset()
        {
            Environment.ResetIteration();
            Controller.Reset();
        }
    }
}
