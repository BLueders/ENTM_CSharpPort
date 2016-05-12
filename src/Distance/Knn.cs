using System.Collections.Generic;
using System.Diagnostics;
using log4net;

namespace ENTM.Distance
{
    public abstract class Knn
    {
        protected readonly ILog _log = LogManager.GetLogger("KNN");


        public interface INeighbour
        {
            double[][] KnnVectors { get; }
            double[] KnnVector { get; }
        }

        private readonly Stopwatch _timer = new Stopwatch();

        public long TimeSpent => _timer.ElapsedMilliseconds;

        protected readonly INeighbour[] _population;
        protected readonly Dictionary<INeighbour, double[]> _neighbourhoods;
        protected readonly int _count;

        protected Knn(INeighbour[] population)
        {
            _population = population;
            _count = _population.Length;
            _neighbourhoods = new Dictionary<INeighbour, double[]>(_count);
        }

        protected abstract void Normalize();
        protected abstract void Train();

        /// <summary>
        /// Initialize KNN with a list of fitness info structs. The supplied start index determines where the algorithm will start comparing values in
        /// the auxFitnessArr array. Use this if not all values of the vector should be considered.
        /// </summary>
        public void Initialize()
        {
            _timer.Restart();

            Normalize();

            Train();

            _timer.Stop();
        }

        public double AverageDistToKnn(INeighbour neighbour, int k)
        {
            double[] neighbourhood = _neighbourhoods[neighbour];

            if (k > neighbourhood.Length)
            {
                _log.Warn($"K was larger than neighbourhood size for KNN (K = {k}, size = {neighbourhood.Length}");
                k = neighbourhood.Length;
            }

            double total = 0;
            for (int i = 0; i < k; i++)
            {
                total += neighbourhood[i];
            }

            return total / k;
        }

        protected double[] GetNeighbourhood(INeighbour neighbour, int count)
        {
            double[] neighbourhood;
            if (!_neighbourhoods.TryGetValue(neighbour, out neighbourhood))
            {
                _neighbourhoods.Add(neighbour, neighbourhood = new double[count - 1]);
            }

            return neighbourhood;
        }
    }
}