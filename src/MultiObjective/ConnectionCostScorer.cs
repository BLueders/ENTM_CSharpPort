using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ENTM.Base;
using ENTM.NoveltySearch;
using SharpNeat.Core;

namespace ENTM.MultiObjective {
    public class ConnectionCostScorer<TGenome> : IObjectiveScorer<TGenome> where TGenome : class, IGenome<TGenome> {
        
        private readonly Stopwatch _timer = new Stopwatch();

        public long TimeSpent => _timer.ElapsedMilliseconds;
        public long TimeSpentAccumulated { get; set; }
        public string Name => "ConnectionCostScorer";
        public MultiObjectiveParameters Params { get; set; }
        public int Objective { get; set; }
        public void Score(IList<Behaviour<TGenome>> behaviours) {

            _timer.Restart();

            int min = int.MaxValue;
            int max = int.MinValue;
            
            foreach (Behaviour<TGenome> behaviour in behaviours) {

                int connections = behaviour.Genome.Position.CoordArray.Length;
                if (connections < min) min = connections;
                if (connections > max) max = connections;
            }

            foreach (Behaviour<TGenome> behaviour in behaviours) {
                int connections = behaviour.Genome.Position.CoordArray.Length;

                double score = (double)(connections - min) / (max - min);
                behaviour.Objectives[Objective] = score;
            }
        }
    }
}