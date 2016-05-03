using System.Collections.Generic;
using SharpNeat.Core;
using ENTM.MultiObjective;

namespace ENTM.NoveltySearch
{
    public interface INoveltyScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        IList<TGenome> Archive { get; }

        bool StopConditionSatisfied { get; }

        void Score(IList<Behaviour<TGenome>> behaviours, int noveltyObjective);
    }
}
