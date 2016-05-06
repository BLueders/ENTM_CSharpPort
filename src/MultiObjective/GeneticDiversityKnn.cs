using System.Collections.Generic;
using System.Diagnostics;
using ENTM.Base;
using ENTM.Distance;
using ENTM.NoveltySearch;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public class GeneticDiversityKnn<TGenome> : IGeneticDiversityScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        private readonly Stopwatch _timer = new Stopwatch();
        public long TimeSpent => _timer.ElapsedMilliseconds;

        internal class GeneticPosition : Knn.INeighbour
        {
            internal Behaviour<TGenome> _behaviour;
            internal double[] _vector; 
            public double[] KnnVector => _vector;
        }

        public MultiObjectiveParameters Params { get; set; }

        private readonly IDistanceMeasure _distanceMeasure = new EuclideanDistanceSquared();

        public void Score(IList<Behaviour<TGenome>> behaviours, int objective)
        {
            _timer.Restart();

            int count = behaviours.Count;
            GeneticPosition[] positions = new GeneticPosition[count];

            for (int i = 0; i < count; i++)
            {
                Behaviour<TGenome> b = behaviours[i];

                GeneticPosition pos = new GeneticPosition();
                pos._behaviour = b;

                KeyValuePair<ulong, double>[] vector = b.Genome.Position.CoordArray;

                // Maximum position (ID) is length, unoccupied positions will be 0, which is fine for distance,
                // as excess or disjoint genes will count towards greater distance.
                int length = (int) vector[vector.Length - 1].Key;

                pos._vector = new double[length];

                foreach (KeyValuePair<ulong, double> pair in vector)
                {
                    pos._vector[pair.Key - 1] = pair.Value;
                }

                positions[i] = pos;
            }

            Knn knn = new Knn(_distanceMeasure);

            knn.Initialize(positions);

            for (int i = 0; i < count; i++)
            {
                GeneticPosition pos = positions[i];

                double score = knn.AverageDistToKnn(pos, Params.GeneticDiversityK);

                pos._behaviour.Objectives[objective] = score;
            }

            _timer.Stop();
        }
    }
}
