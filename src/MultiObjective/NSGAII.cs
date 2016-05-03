using System.Collections.Generic;
using ENTM.NoveltySearch;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    class RankInformation
    {
        public int _dominationCount; //when iterating, we count how many genomes dominate other genomes
        public List<RankInformation> _dominates; //who does this genome dominate
        public int _rank; //what is this genome's rank (i.e. what pareto front is it on)
        public bool _ranked; //has this genome been ranked
        public RankInformation()
        {
            _dominates = new List<RankInformation>();
            reset();
        }
        public void reset()
        {
            _rank = 0;
            _ranked = false;
            _dominationCount = 0;
            _dominates.Clear();
        }
    }

    public class NSGAII<TGenome> : IMultiObjectiveScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        public void Score(IList<Behaviour<TGenome>> behaviours)
        {
            int objectives = behaviours[0].Objectives.Length;
        }
    }
}
