using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public interface IMultiObjectiveScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        void Score(IList<Behaviour<TGenome>> behaviours);
    }
}
