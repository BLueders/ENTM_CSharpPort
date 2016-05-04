using System.Collections.Generic;
using ENTM.NoveltySearch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            _count = _population.Count;
            _objectives = _population[0].Objectives.Length;

            UpdateDominations();

            RankPopulation();

            for (int i = 0; i < _count; i++)
            {
                Behaviour<TGenome> b = _population[i];
                b.MultiObjectiveScore = (b.Rank - 1d) / (_maxRank - 1d);
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
            Assert.AreNotEqual(one, other);

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

                // Iterate in reverse to keep indices correct
                for (int i = count - 1; i >= 0; i--)
                {
                    Behaviour<TGenome> b = unranked[i];

                    if (b.DominatedCount == 0)
                    {
                        // Behaviour is non-dominated, is on the current front
                        b.Rank = rank;
                        unranked.RemoveAt(i);

                        int dCount = b.Dominates.Count;
                        for (int j = 0; j < dCount; j++)
                        {
                            // Decrement dominated count for all behaviours dominated by current front behaviours
                            b.Dominates[j].DominatedCount--;

                            Assert.IsTrue(b.Dominates[j].DominatedCount >= 0);
                        }
                    }
                }

                rank++;
            }

            _maxRank = rank - 1;
        }
    }
}