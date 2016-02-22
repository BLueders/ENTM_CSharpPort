using System;
using System.Threading;
using System.Xml;
using ENTM.TuringMachine;
using ENTM.Utility;
using SharpNeat.Domains;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskEnvironment : IEnvironment
    {
        private FitnessFunction _fitnessFunction;

        // A constant to multiply the fitness with
        private double _fitnessFactor;

        // Determines if the length of the sequence should be fixed or random
        private LengthRule _lengthRule;

        // The length of an element in the sequence (usually M - 1)
        private int _vectorSize;

        // The maximum length that the sequence can be (if random), else the actual sequence length.
        private int _maxSequenceLength;

        private double[][] _sequence;
        public double[][] Sequence => _sequence;

        // Current time step
        private int _step;

        // Current fitness score
        private double _score;

        public IController Controller { get; set; }

        /// <summary>
        /// The size of the bit vector
        /// </summary>
        public int InputCount => _vectorSize;

        /// <summary>
        /// The input we give the controller in each iteration will be empty when we expect the controller to read back the sequence.
        /// The two extra ones are START and DELIMITER bits.
        /// </summary>
        public int OutputCount => _vectorSize + 2;

        public double CurrentScore => _score * _fitnessFactor * (_maxSequenceLength / (1.0 * _sequence.Length));

        //public int MaxScore => (int)(_sequence.Length * 10.0 * (_maxSequenceLength / (1.0 * _sequence.Length)));
        public int MaxScore => (int) (_fitnessFactor * _maxSequenceLength);

        /// <summary>
        /// Terminate when the write and read sequences are complete
        /// </summary>
        public bool IsTerminated => _step >= 2 * _sequence.Length + 2;

        public CopyTaskEnvironment(CopyTaskProperties props)
        {
            _vectorSize = props.VectorSize;
            _maxSequenceLength = props.MaxSequenceLength;
            _lengthRule = props.LengthRule;
            _fitnessFunction = props.FitnessFunction;
            _fitnessFactor = props.FitnessFactor;
        }

        public CopyTaskEnvironment()
        {
            // Default constructor required for generic instantiation
        }

        public void Reset()
        {
            Debug.LogHeader("COPY TASK RESET", true);
        }

        public void Restart()
        {
            Debug.LogHeader("COPY TASK RESTART", true);

            int length;

            switch (_lengthRule)
            {
                case LengthRule.Fixed:
                    length = _maxSequenceLength;
                    break;

                case LengthRule.Random:
                    length = ThreadSafeRandom.Next(0, _maxSequenceLength) + 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CreateSequence(length);

            Debug.Log($"{"Sequence Length:", -16} {length}" +
                      $"\n{"Vector Size:", -16} {_vectorSize}" +
                      $"\n{"Max score:", -16} {MaxScore}", true);

            Debug.Log($"Sequence:\n{Utilities.ToString(_sequence, "f1")}", true);

            _step = 1;
            _score = 0d;
        }

        public double[] InitialObservation => GetObservation(0);

        public double[] PerformAction(double[] action)
        {
            Debug.LogHeader("COPYTASK START", true);
            Debug.Log($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.Log($"{"Step:",-16} {_step}", true);

            double[] result = GetObservation(_step);

            // Compare and score (if reading)
            if (_step >= _sequence.Length + 2)
            {
                // The controllers "action" is the reading after 2 + |seq| steps

                int index = _step - _sequence.Length - 2; // actual sequence index to compare to
                double[] correct = _sequence[index];
                double[] received = action;
                double thisScore = Evaluate(correct, received);
                _score += thisScore;

                Debug.Log($"{"Reading:",-16} {Utilities.ToString(received, "F2")}" +
                            $"\n{"Actual:",-16} {Utilities.ToString(correct, "F2")}" +
                            $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                            $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step - _sequence.Length - 1}" +
                            $"\n{"Max Score:",-16} {_sequence.Length.ToString("F0")}", true);
            }

            Debug.LogHeader("COPYTASK END", true);

            _step++; // Increment step

            return result;
        }


        private void CreateSequence(int length)
        {
            _sequence = new double[length][];
            for (int i = 0; i < _sequence.Length; i++)
            {
                _sequence[i] = new double[_vectorSize];
                for (int j = 0; j < _sequence[i].Length; j++)
                {
                    _sequence[i][j] = ThreadSafeRandom.Next(0, 2);
                }
            }
        }

        private double[] GetObservation(int step)
        {
            double[] observation = new double[_vectorSize + 2];

            if (step == 0)
            {
                Debug.Log($"{"Type:",-16} START", true);
                // Send start vector
                observation[0] = 1; // START bit
            }
            else if (step <= _sequence.Length)
            {
                Debug.Log($"{"Type:",-16} WRITE", true);

                // sending the sequence
                Array.Copy(_sequence[step - 1], 0, observation, 2, _sequence[step - 1].Length);
            }
            else if (step == _sequence.Length + 1)
            {
                Debug.Log($"{"Type:",-16} DELIMITER", true);

                // DELIMITER bit
                observation[1] = 1;
            }
            else
            {
                Debug.Log($"{"Type:",-16} READ", true);
                // When we are reading we just send zeros
            }

            Debug.Log($"{"Observation:",-16} {Utilities.ToString(observation, "f0")}", true);

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
                Array.Copy((Controller as TuringController).TuringMachine.PrevTimeStep.Key, written, target.Length);

                double tmResult = StrictCloseToTarget(target, written);
                double baseResult = StrictCloseToTarget(target, actual);


                Debug.Log($"Target={Utilities.ToString(target, "f4")} Actual={Utilities.ToString(actual, "f4")} Written={Utilities.ToString(written, "f4")} | TM Score={tmResult} Output Score={baseResult}\n", true);


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
    }
}
