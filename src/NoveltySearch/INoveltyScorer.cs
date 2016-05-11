using System.Collections.Generic;
using ENTM.Base;
using SharpNeat.Core;
using ENTM.MultiObjective;
using ENTM.Utility;

namespace ENTM.NoveltySearch
{
    public interface INoveltyScorer<TGenome> : ITimeable where TGenome : class, IGenome<TGenome>
    {
        IList<TGenome> Archive { get; }

        bool StopConditionSatisfied { get; }

        void Score(IList<Behaviour<TGenome>> behaviours, int noveltyObjective);
    }
}
