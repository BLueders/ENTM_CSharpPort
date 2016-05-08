using System;

namespace ENTM.Distance
{
    public class KnnSingleDimension : Knn
    {
        public KnnSingleDimension(INeighbour[] population) : base(population)
        {
        }

        public static KnnSingleDimension Create(Knn.INeighbour[] population)
        {
            if (population == null || population.Length == 0) throw new ArgumentException("Population was null or empty");
            if (population[0].KnnVector == null || population[0].KnnVector.Length == 0) throw new ArgumentException("Zero dimensionality knn vector");

            KnnSingleDimension knn = new KnnSingleDimension(population);

            return knn;
        }

        private double? _vectorMin, _vectorMax;

        public void SetVectorBoundaries(double min, double max)
        {
            _vectorMin = min;
            _vectorMax = max;
        }

        protected override void Train()
        {
            for (int i = 0; i < _count; i++)
            {
                INeighbour neighbour1 = _population[i];
                double[] neighbourhood1 = GetNeighbourhood(neighbour1, _count);
                double[] vector1 = neighbour1.KnnVector;
                int length1 = vector1.Length;

                for (int j = i + 1; j < _count; j++)
                {
                    INeighbour neighbour2 = _population[j];
                    double[] neighbourhood2 = GetNeighbourhood(neighbour2, _count);
                    double[] vector2 = neighbour2.KnnVector;
                    int length2 = vector2.Length;

                    double distance = Distance(vector1, vector2, length1, length2);

                    neighbourhood1[j - 1] = distance;
                    neighbourhood2[i] = distance;
                }

                Array.Sort(neighbourhood1);
            }
        }

        protected override void Normalize()
        {
            // Calculate vector min and max for normalization
            // Check if the vector span has been set manually
            if (_vectorMin == null)
            {
                _vectorMin = double.MaxValue;
                _vectorMax = double.MinValue;

                for (int i = 0; i < _count; i++)
                {
                    INeighbour n = _population[i];
                    int vectorCount = n.KnnVector.Length;
                    for (int j = 0; j < vectorCount; j++)
                    {
                        double v = n.KnnVector[j];
                        if (v < _vectorMin) _vectorMin = v;
                        if (v > _vectorMax) _vectorMax = v;
                    }
                }
            }

            for (int i = 0; i < _count; i++)
            {
                INeighbour n = _population[i];
                int vectorCount = n.KnnVector.Length;
                for (int j = 0; j < vectorCount; j++)
                {
                    double v = n.KnnVector[j];
                    n.KnnVector[j] = (double) ((v - _vectorMin) / (_vectorMax - _vectorMin));
                }
            }
        }

        /// <summary>
        /// Euclidean distance squared
        /// </summary>
        private double Distance(double[] x, double[] y, int lx, int ly)
        {
            // Longest vector
            int length = lx > ly ? lx : ly;

            double distance = 0d;
            for (int i = 0; i < length; i++)
            {
                // Distance
                double vx = i >= lx ? 0d : x[i];
                double vy = i >= ly ? 0d : y[i];
                double d = vy - vx;
                distance += d * d;
                 
            }

            return distance;
        }
    }
}