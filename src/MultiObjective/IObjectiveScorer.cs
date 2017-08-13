using System.Collections.Generic;
using ENTM.Base;
using ENTM.NoveltySearch;
using ENTM.Utility;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public interface IObjectiveScorer<TGenome> : ITimeable where TGenome : class, IGenome<TGenome>
    {
        string Name { get; }
        MultiObjectiveParameters Params { get; set; }
        int Objective { get; set; }
        void Score(IList<Behaviour<TGenome>> behaviours);
    }
}
