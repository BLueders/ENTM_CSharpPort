using System.Xml;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM
{
    public abstract class BaseEvaluator<TEnvironment> : IPhenomeEvaluator<IBlackBox> where TEnvironment : IEnvironment
    {
        public TEnvironment Environment { get; set; }

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
