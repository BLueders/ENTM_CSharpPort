using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.NoveltySearch
{
    public class Knn
    {
        public int K { get; }

        private Stopwatch _timer;

        public long TimeSpent => _timer.ElapsedMilliseconds;

        public Knn(int k)
        {
            K = k;
            _timer = new Stopwatch();
        }

        private Dictionary<FitnessInfo, double[]> neighbourhoods;

        public void Initialize(List<FitnessInfo> behaviourList)
        {
            _timer.Start();
            FitnessInfo[] behaviours = behaviourList.ToArray();


            int count = behaviours.Length;
            int vectorLength = behaviours[0]._auxFitnessArr.Length;
            neighbourhoods = new Dictionary<FitnessInfo, double[]>(count);

            for (int i = 0; i < count; i++)
            {
                FitnessInfo neighbour1 = behaviours[i];
                double[] neighbourhood1 = GetNeighbourhood(neighbour1, count);

                for (int j = i + 1; j < count; j++)
                {
                    FitnessInfo neighbour2 = behaviours[j];
                    double[] neighbourhood2 = GetNeighbourhood(neighbour2, count);

                    // Euclidian distance squared
                    double total = 0;
                    for (int k = 1; k < vectorLength; k++)
                    {
                        double d = neighbour1._auxFitnessArr[k]._value - neighbour2._auxFitnessArr[k]._value;
                        total += d * d;
                        //total += Math.Abs(neighbour1._auxFitnessArr[k]._value - neighbour2._auxFitnessArr[k]._value);
                    }

                    neighbourhood1[j - 1] = total;
                    neighbourhood2[i] = total;
                }

                Array.Sort(neighbourhood1);
            }

            _timer.Stop();
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
