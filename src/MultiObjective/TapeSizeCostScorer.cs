using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ENTM.Base;
using ENTM.NoveltySearch;
using SharpNeat.Core;

namespace ENTM.MultiObjective {
    public class TapeSizeCostScorer<TGenome> : IObjectiveScorer<TGenome> where TGenome : class, IGenome<TGenome> {
        
        private readonly Stopwatch _timer = new Stopwatch();

        public long TimeSpent => _timer.ElapsedMilliseconds;
        public long TimeSpentAccumulated { get; set; }
        public string Name => "TapeSizeCostScorer";
        public MultiObjectiveParameters Params { get; set; }
        public int Objective { get; set; }
        public void Score(IList<Behaviour<TGenome>> behaviours) {

            _timer.Restart();
            
            double min = double.MaxValue;
            double max = double.MinValue;
            
            Dictionary<Behaviour<TGenome>, double> averages = new Dictionary<Behaviour<TGenome>, double>(behaviours.Count);
            foreach (Behaviour<TGenome> behaviour in behaviours) {

                double avg = behaviour.Evaluation.TapeSizes.Average();
                if (avg < min) min = avg;
                if (avg > max) max = avg;

                averages.Add(behaviour, avg);
            }

            foreach (Behaviour<TGenome> behaviour in behaviours) {
                double avg = averages[behaviour];
                double score = (avg - min) / (max - min);
                behaviour.Objectives[Objective] = score;
            }
            
            _timer.Stop();
        }
    }
}