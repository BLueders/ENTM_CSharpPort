using System;
using ENTM.Base;
using ENTM.Replay;

namespace ENTM.Experiments.Xor
{
    public class XorEnvironment : BaseEnvironment
    {
      

        public override IController Controller { get; set; }
        public override int InputCount => 1;
        public override int OutputCount => 2;
        public override double CurrentScore => _score;
        public override double MaxScore => TotalTimeSteps - 1;
        public override double NormalizedScore => _score / (TotalTimeSteps - 1);
        public override bool IsTerminated => _step >= TotalTimeSteps;
        public override int RandomSeed { get; set; }
        public override double[] InitialObservation => GetObservation(0);

        public int SequenceLength { get; set; }

        private double[][] _sequence;

        public override int TotalTimeSteps => _sequence.Length + 1;

        public override int MaxTimeSteps => 5;
        public override int NoveltyVectorLength { get; }
        public override int NoveltyVectorDimensions { get; }
        public override int MinimumCriteriaLength { get; }

        private int _step;
        private double _score;

        public XorEnvironment()
        {
            SequenceLength = 4;
        }

        public override void ResetAll()
        {
            ResetRandom();
        }

        public override void ResetIteration()
        {
            CreateSequence();

            _step = 1;
            _score = 0d;
        }

        private void CreateSequence(int length)
        {
            _sequence = new double[length][];
            for (int i = 0; i < _sequence.Length; i++)
            {
                _sequence[i] = new double[2];

                for (int j = 0; j < 2; j++)
                {
                    _sequence[i][j] = SealedRandom.Next(0, 2);
                }
            }
        }

        private void CreateSequence()
        {
            _sequence = new double[4][];
            _sequence[0] = new[] {0d, 0d};
            _sequence[1] = new[] {0d, 1d};
            _sequence[2] = new[] {1d, 0d};
            _sequence[3] = new[] {1d, 1d};
        }

        public override double[] PerformAction(double[] action)
        {

            double[] prev = _sequence[_step - 1];
            double target = prev[0] == 1d ^ prev[1] == 1d ? 1d : 0d;

            double thisScore = Absolute(target, action[0]);
            _score += thisScore;

            double[] result = GetObservation(_step);

            _step++;

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, result, thisScore);
            }

            return result;
        }

        private double[] GetObservation(int step)
        {
            if (step < TotalTimeSteps - 1)
            {
                return _sequence[step];
            }
            else
            {
                return new double[2];
            }
        }

        private double StrictCloseToTarget(double target, double actual)
        {
            double threshold = 0.25;
            double result = 0.0;

            return 1.0 - Math.Min(threshold, Math.Abs(target - actual)) / threshold;
        }

        private double Absolute(double target, double actual)
        {
            return Math.Abs(target - actual) < .5f ? 1f : 0f;
        }

        public override bool RecordTimeSteps { get; set; }
        private EnvironmentTimeStep _prevTimeStep;

        public override EnvironmentTimeStep InitialTimeStep
        {
            get
            {
                return _prevTimeStep = new EnvironmentTimeStep(new double[InputCount], GetObservation(0), _score);
            }
        }

        public override EnvironmentTimeStep PreviousTimeStep => _prevTimeStep;

       
    }
}
