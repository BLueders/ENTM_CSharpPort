using System;
using System.Threading;
using System.Xml;
using ENTM.MultiObjective;
using ENTM.NoveltySearch;
using ENTM.Replay;
using SharpNeat.Phenomes;
using log4net;

namespace ENTM.Base
{
    public abstract class BaseEvaluator<TEnvironment, TController> : IMultiObjectiveEvaluator<IBlackBox>
        where TEnvironment : IEnvironment
        where TController : IController
    {
        protected readonly ILog _logger = LogManager.GetLogger("Evaluator");

        protected ulong _evaluationCount = 0;
        public ulong EvaluationCount => _evaluationCount;

        public bool StopConditionSatisfied { get; set; }

        public bool NoveltySearchEnabled { get; set; }

        public NoveltySearchParameters NoveltySearchParameters { get; set; }

        public Recorder Recorder;

        public abstract void Initialize(XmlElement properties);

        public abstract int Iterations { get; }
        public abstract int MaxScore { get; }

        public abstract int EnvironmentInputCount { get; }
        public abstract int EnvironmentOutputCount { get; }
        public abstract int ControllerInputCount { get; }
        public abstract int ControllerOutputCount { get; }

        public abstract int NoveltyVectorDimensions { get; }
        public abstract int NoveltyVectorLength { get; }
        public abstract int MinimumCriteriaLength { get; }

        protected abstract void EvaluateObjective(TController controller, int iterations, ref EvaluationInfo evaluation);
        protected abstract void EvaluateNovelty(TController controller, ref EvaluationInfo evaluation);
        protected abstract void EvaluateRecord(TController controller, int iterations, ref EvaluationInfo evaluation);

        private static readonly object _lockEnvironment = new object();
        private static readonly object _lockController = new object();
        private static readonly object _lockNoveltySearchInfo = new object();

        private ThreadLocal<TEnvironment> _environment;

        /// <summary>
        /// ThreadLocal Environment instance
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

        /// <summary>
        /// ThreadLocal Controller instance
        /// </summary>
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

        private ThreadLocal<NoveltySearchInfo> _noveltySearchInfo;

        private NoveltySearchInfo NewNoveltySearchInfo()
        {
            NoveltySearchInfo info = new NoveltySearchInfo();

            info.ScoreNovelty = NoveltySearchEnabled;
            info.VectorMode = NoveltySearchParameters.VectorMode;
            info.NoveltyVectorDimensions = NoveltyVectorDimensions;
            info.NoveltyVectorLength = NoveltyVectorLength;
            info.MinimumCriteriaLength = MinimumCriteriaLength;

            return info;
        }

        /// <summary>
        /// ThreadLocal NoveltySearchInfo instance
        /// </summary>
        protected NoveltySearchInfo NoveltySearchInfo
        {
            get
            {
                lock (_lockNoveltySearchInfo)
                {
                    if (_noveltySearchInfo == null)
                    {
                        _noveltySearchInfo = new ThreadLocal<NoveltySearchInfo>(NewNoveltySearchInfo);
                    }
                    return _noveltySearchInfo.Value;
                }
            }
        }


        /// <summary>
        /// Override must return a new controller instance, which will be used for all evaluations on the current thread
        /// </summary>
        /// <returns>a new instance of TEnvironment</returns>
        protected abstract TController NewController();


        /// <summary>
        /// Override this if we need to do something at the beginning of a new objective evaluation
        /// </summary>
        protected virtual void OnObjectiveEvaluationStart()
        {

        }

        /// <summary>
        /// Override this if we need to do something at the beginning of a new novelty evaluation
        /// </summary>
        protected virtual void OnNoveltyEvaluationStart()
        {

        }

        /// <summary>
        /// If we need to do some initialization, like change the parameters before testing a phenome, we can override this and do it here
        /// </summary>
        protected virtual void SetupTest()
        {

        }

        /// <summary>
        /// Override to setup the environment for generalization testing. Generally this implies testing a longer sequence or lifetime
        /// </summary>
        protected virtual void SetupGeneralizationTest()
        {

        }

        /// <summary>
        /// Override to return the evaluator to its original state after a test (includes generalization tests)
        /// </summary>
        protected virtual void TearDownTest()
        {

        }

        /// <summary>
        /// Main evaluation loop. Called from EA. Runs on separate / multiple threads.
        /// </summary>
        public EvaluationInfo Evaluate(IBlackBox phenome)
        {
            Environment.NoveltySearch = NoveltySearchInfo;
            Controller.NoveltySearch = NoveltySearchInfo;

            // Register the phenome
            Controller.Phenome = phenome;

            // Register controller to environment. Rarely used, since the environment usually does not need to know about the controller
            Environment.Controller = Controller;

            EvaluationInfo evaluation = new EvaluationInfo();

            // Notify subclasses that the objective evaluation is about to start
            OnObjectiveEvaluationStart();

            // Evaluate objective!
            EvaluateObjective(Controller, Iterations, ref evaluation);

            if (NoveltySearchEnabled)
            {
                // Notify subclasses that the novelty evaluation is about to start
                OnNoveltyEvaluationStart();

                // Evaluate novelty!
                EvaluateNovelty(Controller, ref evaluation);
            }

            // Unregister the phenome
            Controller.Phenome = null;

            // Increment evaluation count
            _evaluationCount++;

            // Check if the task has been solved
            if (evaluation.ObjectiveFitness >= MaxScore * .999f)
            {
                _logger.Info($"Max fitness reached: {evaluation.ObjectiveFitness:F4}");
                StopConditionSatisfied = true;
            }

            return evaluation;
        }

        /// <summary>
        /// Evaluate a single phenome. Use this to test a champion. Runs on the main thread.
        /// </summary>
        /// <param name="phenome">The phenome to be tested</param>
        /// <param name="iterations">Number of evaluations</param>
        /// <param name="record">Determines if the evaluation should be recorded</param>
        /// <returns></returns>
        public EvaluationInfo TestPhenome(IBlackBox phenome, int iterations)
        {
            SetupTest();
            return Test(phenome, iterations);
        }

        public EvaluationInfo TestPhenomeGeneralization(IBlackBox phenome, int iterations)
        {
            SetupGeneralizationTest();
            return Test(phenome, iterations);
        }

        private EvaluationInfo Test(IBlackBox phenome, int iterations)
        {
            Controller.NoveltySearch = NoveltySearchInfo;
            Environment.NoveltySearch = NoveltySearchInfo;

            Controller.Phenome = phenome;

            // Deactivate novelty search for testing
            bool novelty = NoveltySearchInfo.ScoreNovelty;
            NoveltySearchInfo.ScoreNovelty = false;

            EvaluationInfo evaluation = new EvaluationInfo();
            evaluation.Iterations = iterations;
            evaluation.ObjectiveFitnessIt = new double[iterations];

            EvaluateRecord(Controller, iterations, ref evaluation);

            CalculateTestStats(ref evaluation);

            TearDownTest();

            NoveltySearchInfo.ScoreNovelty = novelty;

            Controller.Phenome = null;

            return evaluation;
        }

        private void CalculateTestStats(ref EvaluationInfo info)
        {
            double total = 0;
            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = 0; i < info.Iterations; i++)
            {
                double fitness = info.ObjectiveFitnessIt[i];
                total += fitness;

                if (fitness < min) min = fitness;
                if (fitness > max) max = fitness;
            }

            double mean = total / info.Iterations;

            double sdTotal = 0;
            for (int i = 0; i < info.Iterations; i++)
            {
                double fitness = info.ObjectiveFitnessIt[i];

                double v = fitness - mean;
                sdTotal += v * v;
            }

            info.ObjectiveFitnessMean = mean;
            info.ObjectiveFitnessStandardDeviation = Math.Sqrt(sdTotal / info.Iterations);
            info.ObjectiveFitnessMax = max;
            info.ObjectiveFitnessMin = min;
        }

        public void Reset()
        {
            Environment.ResetIteration();
            Controller.Reset();
            if (NoveltySearchEnabled)
            {
                NoveltySearchInfo.Reset();
            }
        }
    }
}
