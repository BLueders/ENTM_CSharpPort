using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.NoveltySearch
{
    public class Knn
    {
        public int K { get; }

        public Knn(int k)
        {
            K = k;
        }

        private Dictionary<FitnessInfo, double[]> neighbourhoods;

        public void Initialize(List<FitnessInfo> behaviours)
        {
            neighbourhoods = new Dictionary<FitnessInfo, double[]>();

            int count = behaviours.Count;
            int vectorLength = behaviours[0]._auxFitnessArr.Length;

            for (int i = 0; i < count; i++)
            {
                FitnessInfo neighbour1 = behaviours[i];
                double[] neighbourhood1 = GetNeighbourhood(neighbour1, count);

                for (int j = i + 1; j < count; j++)
                {
                    FitnessInfo neighbour2 = behaviours[j];
                    double[] neighbourhood2 = GetNeighbourhood(neighbour2, count);

                    double total = 0;
                    for (int k = 1; k < vectorLength; k++)
                    {
                        total += Math.Abs(neighbour1._auxFitnessArr[k]._value - neighbour2._auxFitnessArr[k]._value);
                    }

                    neighbourhood1[j - 1] = total;
                    neighbourhood2[i] = total;
                }

                Array.Sort(neighbourhood1);
            }
        }

        public double AverageDistToKnn(FitnessInfo behaviour)
        {
            double[] neighbourhood = neighbourhoods[behaviour];
            double total = 0;
            for (int i = 0; i < K; i++)
            {
                total += neighbourhood[i];
            }

            return total / K;
        }

        private double[] GetNeighbourhood(FitnessInfo behaviour, int count)
        {
            double[] neighbourhood;
            if (!neighbourhoods.TryGetValue(behaviour, out neighbourhood))
            {
                neighbourhoods.Add(behaviour, neighbourhood = new double[count]);
            }

            return neighbourhood;
        }
    }
}
