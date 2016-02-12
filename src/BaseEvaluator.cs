using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM
{
    abstract class BaseEvaluator<TController> : IPhenomeEvaluator<TController> where TController : IController
    {
        protected ulong _evaluationCount = 0;
        protected bool _stopConditionSatisfied = false;

        public abstract FitnessInfo Evaluate(TController phenome);

        public abstract void Reset();

        public ulong EvaluationCount => _evaluationCount;
        public bool StopConditionSatisfied => _stopConditionSatisfied;
    }
}
