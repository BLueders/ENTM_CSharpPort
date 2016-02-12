using System;
using System.Xml;
using ENTM.TuringMachine;
using SharpNeat.Domains;

namespace ENTM.Experiments.CopyTask
{
    class CopyTaskEnvironment : IEnvironment
    {
        private FitnessFunction fitnessFunction;

        private int _vectorLength;
        private int _sequenceLength;
        private double[][] Sequence;

        private int _step;
        private double _score;

        public IController Controller { get; set; }

        public int InputCount => _vectorLength;

        public int OutputCount => _vectorLength + 2;

        public void Initialize(XmlElement xmlConfig)
        {
            _vectorLength = XmlUtils.GetValueAsInt(xmlConfig, "VectorLength");
            _sequenceLength = XmlUtils.GetValueAsInt(xmlConfig, "SequenceLength");
            
            Reset();
        }

        public void Reset()
        {

        }

        public void Restart()
        {
            CreateSequence();

            _step = 1;
            _score = 0d;
        }

        public double[] InitialObservation => GetObservation(0);

        public double[] PerformAction(double[] action)
        {
            if (Debug.On)
            {
                Console.WriteLine("-------------------------- COPYTASK --------------------------");
            }

            double[] result = GetObservation(_step);

            // Compare and score (if reading)
            if (_step > Sequence.Length + 2)
            {
                // The controllers "action" is the reading after 2 + |seq| steps

                int index = _step - Sequence.Length - 2 - 1; // actual sequence index to compare to
                double[] correct = Sequence[index];
                double[] received = action;
                double thisScore = Evaluate(correct, received);
                _score += thisScore;

                if (Debug.On) Console.WriteLine("\tReading: " + Utilities.ToString(received) + " compared to " + Utilities.ToString(correct) + " = " + thisScore);
            }

            if (Debug.On) Console.WriteLine("--------------------------------------------------------------");

            _step++; // Increment step

            return result;
        }

        public double CurrentScore => _score;

        public int MaxScore { get; }
        public bool IsTerminated { get; }

        private void CreateSequence()
        {
            Sequence = new double[_sequenceLength][];
            for (int i = 0; i < Sequence.Length; i++)
            {
                Sequence[i] = new double[_vectorLength];
                for (int j = 0; j < Sequence[i].Length; j++)
                {
                    Sequence[i][j] = ThreadSafeRandom.Next(0, 2);
                }
            }
        }

        private double[] GetObservation(int step)
        {
            double[] observation = new double[_vectorLength + 2];

            if (step == 0)
            {
                // Send start vector
                observation[0] = 1; // START bit

            }
            else if (step <= Sequence.Length)
            {
                // sending the sequence
                Array.Copy(Sequence[step - 1], 0, observation, 2, Sequence[step - 1].Length);
            }
            else if (step == Sequence.Length + 1)
            {
                // DELIMITER bit
                observation[1] = 1;

            }
            else {
                // When we are reading we just send zeros
            }

            if (Debug.On) Console.WriteLine(step + ": " + Utilities.ToString(observation, "0f"));

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
            switch (fitnessFunction)
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
                Array.Copy((Controller as TuringController).TuringMachine.LastTimeStep.Key, written, target.Length);

                double tmResult = StrictCloseToTarget(target, written);
                double baseResult = StrictCloseToTarget(target, actual);

                if (Debug.On)
                {
                    Console.WriteLine("Target=%s Actual=%s Written=%s | TM Score=%.2f Output Score=%.2f\n", Utilities.ToString(target, "4f"), Utilities.ToString(actual, "4f"), Utilities.ToString(written, "4f"), tmResult, baseResult);
                }

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
