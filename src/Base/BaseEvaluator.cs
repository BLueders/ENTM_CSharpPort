using System.Threading;
using System.Xml;
using ENTM.MultiObjective;
using ENTM.NoveltySearch;
using ENTM.Replay;
using SharpNeat.Phenomes;

namespace ENTM.Base
{
    public abstract class BaseEvaluator<TEnvironment, TController> : IMultiObjectiveEvaluator<IBlackBox>
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
        public abstract int MinimumCriteriaLength { get; }

        public abstract EvaluationInfo Evaluate(TController controller, int iterations, bool record);

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
        public EvaluationInfo Evaluate(IBlackBox phenome)
        {
            OnEvaluationStart();

            EvaluationInfo evaluation = Evaluate(phenome, Iterations, false);

            _evaluationCount++;

            if (evaluation.ObjectiveFitness >= MaxScore * .999f)
            {
                _stopConditionSatisfied = true;
            }

            return evaluation;
        }

        /// <summary>
        /// Evaluate a single phenome only once. Use this to test a champion. Runs on the main thread.
        /// </summary>
        /// <param name="phenome">The phenome to be tested</param>
        /// <param name="iterations">Number of evaluations</param>
        /// <param name="record">Determines if the evaluation should be recorded</param>
        /// <returns></returns>
        public EvaluationInfo TestPhenome(IBlackBox phenome, int iterations)
        {
            SetupTest();

            EvaluationInfo evaluation = Evaluate(phenome, iterations, true);

            TearDownTest();

            return evaluation;
        }

        private EvaluationInfo Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            // Register the phenome
            Controller.Phenome = phenome;

            Controller.NoveltySearch.ScoreNovelty = NoveltySearchEnabled;
            Controller.NoveltySearch.VectorMode = NoveltySearchParameters.VectorMode;
            Controller.NoveltySearch.NoveltyVectorLength = NoveltyVectorLength;
            Controller.NoveltySearch.MinimumCriteriaLength = MinimumCriteriaLength;

            Environment.ResetAll();
            Environment.Controller = Controller;

            // Evaluate the controller / phenome
            EvaluationInfo evaluation = Evaluate(Controller, iterations, record);

            // Unregister the phenome
            Controller.Phenome = null;

            return evaluation;
        }

        public void Reset()
        {
            Environment.ResetIteration();
            Controller.Reset();
        }
    }
}
