using System;
using ENTM.Replay;
using ENTM.TuringMachine;
using ENTM.Utility;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskEnvironment : BaseEnvironment
    {
        private readonly FitnessFunction _fitnessFunction;

        // Determines if the length of the sequence should be fixed or random
        public LengthRule LengthRule { get; set; }

        // The length of an element in the sequence (usually M - 1)
        private readonly int _vectorSize;

        // The maximum length that the sequence can be (if random), else the actual sequence length.
        public int MaxSequenceLength;

        public double[][] Sequence { get; private set; }

        // Current time step
        private int _step;

        // Current fitness score
        private double _score;

        public override IController Controller { get; set; }

        /// <summary>
        /// The size of the bit vector
        /// </summary>
        public override int InputCount => _vectorSize;

        /// <summary>
        /// The input we give the controller in each iteration will be empty when we expect the controller to read back the sequence.
        /// The two extra ones are START and DELIMITER bits.
        /// </summary>
        public override int OutputCount => _vectorSize + 2;

        public override double CurrentScore => _score * ((double) MaxSequenceLength / (double) Sequence.Length);

        public override double MaxScore => MaxSequenceLength;

        public override double NormalizedScore => CurrentScore / MaxScore;

        /// <summary>
        /// Terminate when the write and read sequences are complete
        /// </summary>
        public override bool IsTerminated => _step >= TotalTimeSteps;

        /// <summary>
        /// Read + Write + start and delimiter
        /// </summary>
        public override int TotalTimeSteps => 2 * Sequence.Length + 2;

        public sealed override int RandomSeed { get; set; }

        public CopyTaskEnvironment(CopyTaskProperties props)
        {
            _vectorSize = props.VectorSize;
            MaxSequenceLength = props.MaxSequenceLength;
            LengthRule = props.LengthRule;
            _fitnessFunction = props.FitnessFunction;
            RandomSeed = props.RandomSeed;
        }

        public override void ResetAll()
        {
            Debug.DLogHeader("COPY TASK RESET ALL", true);
            ResetRandom();
        }

        public override void ResetIteration()
        {
            Debug.DLogHeader("COPY TASK NEW ITERATION", true);

            int length;

            switch (LengthRule)
            {
                case LengthRule.Fixed:
                    length = MaxSequenceLength;
                    break;

                case LengthRule.Random:
                    length = ThreadSafeRandom.Next(0, MaxSequenceLength) + 1;
                    //length = random.Next(0, _maxSequenceLength) + 1;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CreateSequence(length);

            Debug.DLog($"{"Sequence Length:", -16} {length}" +
                      $"\n{"Vector Size:", -16} {_vectorSize}" +
                      $"\n{"Max score:", -16} {MaxScore}", true);

            Debug.DLog($"Sequence:\n{Utilities.ToString(Sequence, "f1")}", true);

            _step = 1;
            _score = 0d;
        }

        public override double[] InitialObservation => GetObservation(0);

        public override double[] PerformAction(double[] action)
        {
            Debug.DLogHeader("COPYTASK START", true);
            Debug.DLog($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.DLog($"{"Step:",-16} {_step}", true);

            double[] result = GetObservation(_step);

            double thisScore = 0;
            // Compare and score (if reading)
            if (_step >= Sequence.Length + 2)
            {
                // The controllers "action" is the reading after 2 + |seq| steps

                int index = _step - Sequence.Length - 2; // actual sequence index to compare to
                double[] correct = Sequence[index];
                double[] received = action;
                thisScore = Evaluate(correct, received);
                _score += thisScore;

                Debug.DLog($"{"Reading:",-16} {Utilities.ToString(received, "F2")}" +
                            $"\n{"Actual:",-16} {Utilities.ToString(correct, "F2")}" +
                            $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                            $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step - Sequence.Length - 1}" +
                            $"\n{"Max Score:",-16} {Sequence.Length.ToString("F0")}", true);
            }

            Debug.DLogHeader("COPYTASK END", true);

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, result, thisScore);
            }

            _step++; // Increment step

            return result;
        }


        private void CreateSequence(int length)
        {
            Sequence = new double[length][];
            for (int i = 0; i < Sequence.Length; i++)
            {
                bool hasOnes = false;
                Sequence[i] = new double[_vectorSize];
                for (int j = 0; j < Sequence[i].Length; j++)
                {
                    double value = SealedRandom.Next(0, 2);
                    if (value == 1) hasOnes = true;
                    Sequence[i][j] = value;
                }
            }
        }

        private double[] GetObservation(int step)
        {
            double[] observation = new double[_vectorSize + 2];

            if (step == 0)
            {
                Debug.DLog($"{"Type:",-16} START", true);
                // Send start vector
                observation[0] = 1; // START bit
            }
            else if (step <= Sequence.Length)
            {
                Debug.DLog($"{"Type:",-16} WRITE", true);

                // sending the sequence
                Array.Copy(Sequence[step - 1], 0, observation, 2, Sequence[step - 1].Length);
            }
            else if (step == Sequence.Length + 1)
            {
                Debug.DLog($"{"Type:",-16} DELIMITER", true);

                // DELIMITER bit
                observation[1] = 1;
            }
            else
            {
                Debug.DLog($"{"Type:",-16} READ", true);
                // When we are reading we just send zeros
            }

            Debug.DLog($"{"Observation:",-16} {Utilities.ToString(observation, "f0")}", true);

            return observation;
        }

        protected double Evaluate(double[] correct, double[] received)
        {
            return CalcSimilarity(correct, received);
        }

        #region FitnessFunctions

        /**
	     * Calculates how similar the two vectors are as a value between 0.0 and 1.0;
	     */

        private double CalcSimilarity(double[] first, double[] second)
        {
            switch (_fitnessFunction)
            {
                case FitnessFunction.PartialScore:
                    return PartialScore(first, second);
                case FitnessFunction.Emilarity:
                    return Utilities.Emilarity(first, second);
                case FitnessFunction.ClosestBinary:
                    return ClosestBinary(first, second);
                case FitnessFunction.CompleteBinary:
                    return CompleteMatchClosestBinary(first, second);
                case FitnessFunction.StrictCloseToTarget:
                    return StrictCloseToTarget(first, second); // "strict-close"
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        private double PartialScore(double[] target, double[] actual)
        {
            if (Controller is TuringController)
            {
                // Get half the score for storing correctly
                // comparing the target with the first E elements written to memory that round
                double[] written = new double[target.Length];
                Array.Copy(((Controller as TuringController).TuringMachine as IReplayable<TuringMachineTimeStep>).PreviousTimeStep.Key, written, target.Length);

                double tmResult = StrictCloseToTarget(target, written);
                double baseResult = StrictCloseToTarget(target, actual);


                Debug.DLog($"Target={Utilities.ToString(target, "f4")} Actual={Utilities.ToString(actual, "f4")} Written={Utilities.ToString(written, "f4")} | TM Score={tmResult} Output Score={baseResult}\n", true);


                return 0.5 * tmResult + 0.5 * baseResult;
            }
            else
            {
                throw new InvalidOperationException("Can not use partial-score without the Controller being of type TuringController and its TM being MinimalTuringMachine");
            }
        }

        /// <summary>
        /// Score values that are closer to the correct values, within a threshold.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        private double StrictCloseToTarget(double[] target, double[] actual)
        {
            double threshold = 0.25;
            double result = 0.0;

            for (int i = 0; i < target.Length; i++)
            {
                result += 1.0 - Math.Min(threshold, Math.Abs(target[i] - actual[i])) / threshold;
            }

            return result / target.Length;
        }

        /// <summary>
        /// Assuming the targets are binary (e.g. either 0.0 or 1.0)
        /// Score values that are closest to the correct value
        /// </summary>
        /// <param name="target"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        private double ClosestBinary(double[] target, double[] actual)
        {
            double result = 0;
            for (int i = 0; i < target.Length; i++)
            {
                if (Math.Abs(target[i] - actual[i]) < 0.5)
                {
                    result++;
                }
            }
            return result / target.Length;
        }

        /// <summary>
        /// Only score if all values are closest to the correct values
        /// </summary>
        /// <param name="target"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        private double CompleteMatchClosestBinary(double[] target, double[] actual)
        {
            int matches = 0;
            for (int i = 0; i < target.Length; i++)
            {
                if (Math.Abs(target[i] - actual[i]) < 0.5)
                {
                    matches++;
                }
            }
            return matches == target.Length ? 1 : 0;
        }

        #endregion

        #region Replayable

        private EnvironmentTimeStep _prevTimeStep;

        public override bool RecordTimeSteps { get; set; } = false;

        public override EnvironmentTimeStep InitialTimeStep
        {
            get
            {
                return _prevTimeStep = new EnvironmentTimeStep(new double[InputCount], GetObservation(0), _score);
            }
        }

        public override EnvironmentTimeStep PreviousTimeStep => _prevTimeStep;

        #endregion
    }

}
