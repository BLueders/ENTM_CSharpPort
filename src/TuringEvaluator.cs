using System.Xml;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Threading;
using ENTM.Replay;
using ENTM.TuringMachine;

namespace ENTM
{
    public abstract class TuringEvaluator<TEnvironment> : IPhenomeEvaluator<IBlackBox> where TEnvironment : IEnvironment
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

        private TuringMachineProperties _turingMachineProps;
        public TuringMachineProperties TuringMachineProperties => _turingMachineProps;

        protected ulong _evaluationCount = 0;
        protected bool _stopConditionSatisfied = false;

        public abstract double Evaluate(IBlackBox phenome, int iterations, bool record);
        public abstract int Iterations { get; }
        public abstract void Reset();

        public Recorder Recorder;

        public ulong EvaluationCount => _evaluationCount;
        public bool StopConditionSatisfied => _stopConditionSatisfied;

        public abstract int MaxScore { get; }

        public abstract int EnvironmentInputCount { get; }

        public abstract int EnvironmentOutputCount { get; }

        public int TuringMachineOutputCount => _turingMachineProps.M * _turingMachineProps.Heads;

        public int TuringMachineInputCount
        {
            get
            {
                int shifts = 0;
                switch (_turingMachineProps.ShiftMode)
                {
                    case ShiftMode.Multiple:
                        shifts = _turingMachineProps.ShiftLength;
                        break;
                    case ShiftMode.Single:
                        shifts = 1;
                        break;
                }

                return (_turingMachineProps.M + 2 + shifts) * _turingMachineProps.Heads;
            }
        }


        public virtual void Initialize(XmlElement xmlConfig)
        {
            _turingMachineProps = new TuringMachineProperties(xmlConfig.SelectSingleNode("TuringMachineParams") as XmlElement);
        }

        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            Environment.ResetAll();

            double score = Evaluate(phenome, Iterations, false);

            _evaluationCount++;

            if (score >= MaxScore) _stopConditionSatisfied = true;

            return new FitnessInfo(score, 0);
        }
    }
}
