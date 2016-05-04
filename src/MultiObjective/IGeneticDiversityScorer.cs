using System.Collections.Generic;
using ENTM.NoveltySearch;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public interface IGeneticDiversityScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        MultiObjectiveParameters Params { get; set; }
        void Score(IList<Behaviour<TGenome>> behaviours, int objective);
    }
}
