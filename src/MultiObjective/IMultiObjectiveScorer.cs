using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public interface IMultiObjectiveBehaviour
    {
        // The final fitness calculated by the Multi Objective algorithm (if applied)
        double MultiObjectiveScore { get; set; }

        // The objective scores to be considered
        double[] Objectives { get; set; }

        // The rank of this behaviour, i.e. which Pareto front is it on
        int Rank { get; set; }

        // List of behaviours dominated by this behaviour
        IList<IMultiObjectiveBehaviour> Dominates { get; }

        // How many behaviours dominate this behaviour
        int DominatedCount { get; set; }
    }

    public interface IMultiObjectiveScorer
    {
        void Score(IList<IMultiObjectiveBehaviour> behaviours);
    }
}
