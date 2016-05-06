using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ENTM.Distance
{
    public class Knn
    {
        public interface INeighbour
        {
            double[] KnnVector { get; }
        }

        private Stopwatch _timer;

        public long TimeSpent => _timer.ElapsedMilliseconds;

        private Dictionary<INeighbour, double[]> _neighbourhoods;

        private readonly IDistanceMeasure _distanceMeasure;

        public Knn(IDistanceMeasure distanceMeasure)
        {
            _distanceMeasure = distanceMeasure;
        }

        /// <summary>
        /// Initialize KNN with a list of fitness info structs. The supplied start index determines where the algorithm will start comparing values in
        /// the auxFitnessArr array. Use this if not all values of the vector should be considered.
        /// </summary>
        public void Initialize(INeighbour[] population)
        {
            _timer = new Stopwatch();
            _timer.Start();

            int count = population.Length;
            _neighbourhoods = new Dictionary<INeighbour, double[]>(count);

            for (int i = 0; i < count; i++)
            {
                INeighbour neighbour1 = population[i];
                double[] neighbourhood1 = GetNeighbourhood(neighbour1, count);
                double[] vector1 = neighbour1.KnnVector;
                int vectorLength1 = vector1.Length;

                for (int j = i + 1; j < count; j++)
                {
                    INeighbour neighbour2 = population[j];
                    double[] neighbourhood2 = GetNeighbourhood(neighbour2, count);
                    double[] vector2 = neighbour2.KnnVector;
                    int vectorLength2 = vector2.Length;

                    double distance = _distanceMeasure.Distance(vector1, vector2, vectorLength1, vectorLength2);

                    neighbourhood1[j - 1] = distance;
                    neighbourhood2[i] = distance;
                }

                Array.Sort(neighbourhood1);
            }

            _timer.Stop();
        }

        public double AverageDistToKnn(INeighbour neighbour, int k)
        {
            double[] neighbourhood = _neighbourhoods[neighbour];
            double total = 0;
            for (int i = 0; i < k; i++)
            {
                total += neighbourhood[i];
            }

            return total / k;
        }

        private double[] GetNeighbourhood(INeighbour neighbour, int count)
        {
            double[] neighbourhood;
            if (!_neighbourhoods.TryGetValue(neighbour, out neighbourhood))
            {
                _neighbourhoods.Add(neighbour, neighbourhood = new double[count-1]);
            }

            return neighbourhood;
        }
    }
}