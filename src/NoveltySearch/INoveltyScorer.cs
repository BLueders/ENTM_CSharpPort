using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.NoveltySearch
{
    public interface INoveltyScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        IList<TGenome> Archive { get; }

        void Score(IList<Behaviour<TGenome>> behaviours);
    }
}
