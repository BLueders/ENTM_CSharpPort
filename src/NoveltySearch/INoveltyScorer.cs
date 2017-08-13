using System.Collections.Generic;
using ENTM.Base;
using SharpNeat.Core;
using ENTM.MultiObjective;
using ENTM.Utility;

namespace ENTM.NoveltySearch
{
    public interface INoveltyScorer<TGenome> : IObjectiveScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        IList<TGenome> Archive { get; }

        bool StopConditionSatisfied { get; }
    }
}
