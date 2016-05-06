using System.Collections.Generic;
using ENTM.Base;
using ENTM.NoveltySearch;
using ENTM.Utility;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public interface IGeneticDiversityScorer<TGenome> : ITimeable where TGenome : class, IGenome<TGenome>
    {
        MultiObjectiveParameters Params { get; set; }
        void Score(IList<Behaviour<TGenome>> behaviours, int objective);
    }
}
