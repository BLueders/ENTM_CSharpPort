using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM.Experiments.Sine
{
    /// <summary>
    /// Class used to evaluate neural networks that emulate a sine function
    /// </summary>
    public class SineEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        private ulong _evaluationCount;
        private bool _stopConditionSatisfied;

        private readonly int _testCount;

        public SineEvaluator(int testCount)
        {
            _testCount = testCount;
            _evaluationCount = 0;
            _stopConditionSatisfied = false;
        }

        #region IPhenomeEvaluator<IBlackBox> Members

        /// <summary>
        /// Gets the total number of evaluations that have been performed.
        /// </summary>
        public ulong EvaluationCount => _evaluationCount;

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the the evolutionary algorithm/search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied => _stopConditionSatisfied;

        /// <summary>
        /// Evaluate the provided IBlackBox against the random tic-tac-toe player and return its fitness score.
        /// Each network plays 10 games against the random player and two games against the expert player.
        /// Half of the games are played as circle and half are played as x.
        /// 
        /// A win is worth 10 points, a draw is worth 1 point, and a loss is worth 0 points.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox box)
        {
            double fitness = 0;

            _evaluationCount++;

            if (fitness >= _testCount) _stopConditionSatisfied = true;

            // Return the fitness score
            return new FitnessInfo(fitness, fitness);
        }

        /// <summary>
        /// ResetAll the internal state of the evaluation scheme if any exists.
        /// Note. This method does nothing.
        /// </summary>
        public void Reset()
        {
        }
        #endregion
    }
}
