using System;
using System.Collections.Generic;
using System.Diagnostics;
using ENTM.Utility;

namespace ENTM.Distance
{
    public class KnnMultiDimensional : Knn
    {
        private KnnMultiDimensional(INeighbour[] population, int dimensions) : base(population)
        {
            _vectorDimensions = population[0].KnnVectors[0].Length;
            _vectorMins = new double?[_vectorDimensions];
            _vectorMaxs = new double?[_vectorDimensions];
        }

        public static KnnMultiDimensional Create(INeighbour[] population, int dimensions)
        {
            if (population == null || population.Length == 0) throw new ArgumentException("Population was null or empty");
            if (population[0].KnnVectors == null || population[0].KnnVectors.Length == 0) throw new ArgumentException("Knn vectors not instantiated");
            if (population[0].KnnVectors[0] == null || population[0].KnnVectors[0].Length == 0) throw new ArgumentException("Zero dimensionality knn vector");

            KnnMultiDimensional knn = new KnnMultiDimensional(population, dimensions);

            return knn;
        }

        private readonly int _vectorDimensions;

        private readonly double?[] _vectorMins, _vectorMaxs;

        /// <summary>
        /// If a vector span is known beforehand, set it before training to reduce computational cost
        /// </summary>
        public void SetVectorBoundaries(int i, double min, double max)
        {
            _vectorMins[i] = min;
            _vectorMaxs[i] = max;
        }

        /// <summary>
        /// Initialize KNN with a list of fitness info structs. The supplied start index determines where the algorithm will start comparing values in
        /// the auxFitnessArr array. Use this if not all values of the vector should be considered.
        /// </summary>
        protected override void Train()
        {
            for (int i = 0; i < _count; i++)
            {
                INeighbour neighbour1 = _population[i];
                double[] neighbourhood1 = GetNeighbourhood(neighbour1, _count);
                double[][] vector1 = neighbour1.KnnVectors;

                for (int j = i + 1; j < _count; j++)
                {
                    INeighbour neighbour2 = _population[j];
                    double[] neighbourhood2 = GetNeighbourhood(neighbour2, _count);
                    double[][] vector2 = neighbour2.KnnVectors;

                    double distance = Distance(vector1, vector2);

                    neighbourhood1[j - 1] = distance;
                    neighbourhood2[i] = distance;
                }

                Array.Sort(neighbourhood1);
            }
        }


        protected override void Normalize()
        {
            // Calculate vector min and max for normalization
            for (int i = 0; i < _vectorDimensions; i++)
            {
                // Check if the vector span has been set manually
                if (_vectorMins[i] != null) continue;

                _vectorMins[i] = double.MaxValue;
                _vectorMaxs[i] = double.MinValue;

                for (int j = 0; j < _count; j++)
                {
                    INeighbour n = _population[j];
                    int vectorCount = n.KnnVectors.Length;
                    for (int k = 0; k < vectorCount; k++)
                    {
                        double v = n.KnnVectors[k][i];
                        if (v < _vectorMins[i]) _vectorMins[i] = v;
                        if (v > _vectorMaxs[i]) _vectorMaxs[i] = v;
                    }
                }
            }

            for (int i = 0; i < _vectorDimensions; i++)
            {
                for (int j = 0; j < _count; j++)
                {
                    INeighbour n = _population[j];
                    int vectorCount = n.KnnVectors.Length;
                    for (int k = 0; k < vectorCount; k++)
                    {
                        double v = n.KnnVectors[k][i];
                        n.KnnVectors[k][i] = (double) ((v - _vectorMins[i]) / (_vectorMaxs[i] - _vectorMins[i]));
                    }
                }
            }
        }
        
        /// <summary>
        /// Euclidean distance squared
        /// </summary>
        private double Distance(double[] x, double[] y)
        {
            double distance = 0d;
            for (int i = 0; i < _vectorDimensions; i++)
            {
                // Distance
                double d = y[i] - x[i];
                distance += d * d;
            }

            return distance;
        }

        private double Distance(double[][] x, double[][] y)
        {
            int lx = x.Length;
            int ly = y.Length;

            // Longest vector
            int length = lx > ly ? lx : ly;

            double distance = 0d;
            for (int i = 0; i < length; i++)
            {
                // Zero pad short vectors
                double[] vx = i >= lx ? new double[_vectorDimensions] : x[i];
                double[] vy = i >= ly ? new double[_vectorDimensions] : y[i];

                distance += Distance(vx, vy);
            }

            return distance;
        }

    
    }
}