using System.Collections.Generic;
using ENTM.Utility;

namespace ENTM.MultiObjective
{
    public interface IMultiObjectiveScorer : ITimeable
    {
        void Score(IList<IMultiObjectiveBehaviour> behaviours);

        /// <summary>
        /// Returns the number of Pareto Optimal behaviours after a given run.
        /// </summary>
        int ParetoOptimal { get; }
    }

    public interface IMultiObjectiveBehaviour
    {
        /// <summary>
        /// The final fitness calculated by the Multi Objective algorithm (if applied)
        /// </summary>
        double MultiObjectiveScore { get; set; }

        /// <summary>
        /// The objective scores to be considered
        /// </summary>
        double[] Objectives { get; set; }

        /// <summary>
        /// The rank of this behaviour, i.e. which Pareto front is it on
        /// </summary>
        int Rank { get; set; }

        /// <summary>
        /// The distance to the nearest behaviours in objective space
        /// </summary>
        double CrowdingDistance { get; set; }

        /// <summary>
        /// List of behaviours dominated by this behaviour
        /// </summary>
        IList<IMultiObjectiveBehaviour> Dominates { get; set; }

        /// <summary>
        /// How many behaviours dominate this behaviour
        /// </summary>
        int DominatedCount { get; set; }

        /// <summary>
        /// Reject a behaviour based on too high behavioural similarity
        /// </summary>
        void Reject();

        /// <summary>
        /// Reset the state of the behaviour for a new comparison. Usually only for elite behaviours.
        /// </summary>
        void Reset();
    }

    public class ObjectiveComparer : IComparer<IMultiObjectiveBehaviour>
    {
        public int Objective { get; set; }

        public int Compare(IMultiObjectiveBehaviour x, IMultiObjectiveBehaviour y)
        {
            double ox = x.Objectives[Objective];
            double oy = y.Objectives[Objective];

            if (ox > oy) return 1;
            if (ox < oy) return -1;
            return 0;
        }
    }

    public class PopulationComparer : IComparer<IMultiObjectiveBehaviour>
    {
        public int Compare(IMultiObjectiveBehaviour x, IMultiObjectiveBehaviour y)
        {
            // Minimize rank
            if (x.Rank < y.Rank) return 1;
            if (x.Rank > y.Rank) return -1;

            // Equal ranks, sort by crowding distance
            // Maximize crowding distance
            if (x.CrowdingDistance > y.CrowdingDistance) return 1;
            if (x.CrowdingDistance < y.CrowdingDistance) return -1;

            return 0;
        }
    }
}