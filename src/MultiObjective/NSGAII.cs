using System.Collections.Generic;
using ENTM.NoveltySearch;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{

    public class NSGAII<TGenome> : IMultiObjectiveScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        private List<Behaviour<TGenome>> _population;
        private int _count;
        private int _objectives;
        private int _maxRank;

        public void Score(IList<Behaviour<TGenome>> behaviours)
        {
            _population = new List<Behaviour<TGenome>>(behaviours);
            _count = behaviours.Count;
            _objectives = behaviours[0].Objectives.Length;

            UpdateDominations();

            RankPopulation();

            for (int i = 0; i < _count; i++)
            {
                Behaviour<TGenome> b = _population[i];
            }
        }

        private void UpdateDominations()
        {
            // Compare all bahaviours to each other
            for (int i = 0; i < _count; i++)
            {
                Behaviour<TGenome> one = _population[i];
                for (int j = i + 1; j < _count; j++)
                {
                    Behaviour<TGenome> other = _population[j];

                    if (Dominates(one, other))
                    {
                        one.Dominates.Add(other);
                        other.DominatedCount++;
                    }
                    else if (Dominates(other, one))
                    {
                        other.Dominates.Add(one);
                        one.DominatedCount++;
                    }
                }
            }
        }

        private bool Dominates(Behaviour<TGenome> one, Behaviour<TGenome> other)
        {
            bool dominates = false;

            // Compare all objectives
            for (int i = 0; i < _objectives; i++)
            {
                double oOne = one.Objectives[i];
                double oOther = other.Objectives[i];

                if (oOne < oOther)
                {
                    // Objective i is worse, one does not dominate other
                    return false;
                }
                if (oOne > oOther)
                {
                    // Objective i is better, potential domination
                    dominates = true;
                }
            }

            return dominates;
        }

        private void RankPopulation()
        {
            List<Behaviour<TGenome>> unranked = new List<Behaviour<TGenome>>(_population);

            int rank = 1;

            // All behaviours must be ranked
            while (unranked.Count > 0)
            {
                int count = unranked.Count;

                for (int i = 0; i < count; i++)
                {
                    Behaviour<TGenome> b = _population[i];

                    // Behaviour is non-dominated, is on the current front
                    if (b.DominatedCount == 0)
                    {
                        unranked.RemoveAt(i);
                        b.Rank = rank;

                        int dCount = b.Dominates.Count;
                        for (int j = 0; j < dCount; j++)
                        {
                            // Decrement dominated count for all behaviours dominated by current front behaviours
                            b.Dominates[j].DominatedCount--;
                        }
                    }
                }

                rank++;
            }

            _maxRank = rank - 1;
        }
    }
}