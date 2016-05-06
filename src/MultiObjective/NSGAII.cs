using System.Collections.Generic;
using System.Text;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{

    public class NSGAII : IMultiObjectiveScorer
    {
        private readonly ILog _logger = LogManager.GetLogger("NSGAII");

        private List<IMultiObjectiveBehaviour> _population;
        private int _count;
        private int _objectives;
        private int _maxRank;

        private Dictionary<int, double> _maxObjectiveScores; 

        public void Score(IList<IMultiObjectiveBehaviour> behaviours)
        {
            _population = new List<IMultiObjectiveBehaviour>(behaviours);
            _count = _population.Count;
            _objectives = _population[0].Objectives.Length;

            _maxObjectiveScores = new Dictionary<int, double>(_objectives);

            UpdateDominations();

            StringBuilder sb = new StringBuilder("Max Objectives:");
            for (int i = 0; i < _objectives; i++)
            {
                sb.Append($" {i}: {_maxObjectiveScores[i]:F4}");
            }
            _logger.Info(sb.ToString());

            RankPopulation();

            for (int i = 0; i < _count; i++)
            {
                IMultiObjectiveBehaviour b = _population[i];
                b.MultiObjectiveScore = (b.Rank - 1d) / (_maxRank - 1d);
            }
        }

        private void UpdateMaxObjectiveScores(double[] objectives)
        {
            for (int i = 0; i < _objectives; i++)
            {
                double score = objectives[i];
                double max;
                if (!_maxObjectiveScores.TryGetValue(i, out max))
                {
                    _maxObjectiveScores.Add(i, score);
                }
                else
                {
                    if (score > _maxObjectiveScores[i])
                    {
                        _maxObjectiveScores[i] = score;
                    }
                }
            }
        }

        private void UpdateDominations()
        {
            // Compare all bahaviours to each other
            for (int i = 0; i < _count; i++)
            {
                IMultiObjectiveBehaviour one = _population[i];

                UpdateMaxObjectiveScores(one.Objectives);

                for (int j = i + 1; j < _count; j++)
                {
                    IMultiObjectiveBehaviour other = _population[j];

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

        private bool Dominates(IMultiObjectiveBehaviour one, IMultiObjectiveBehaviour other)
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
            List<IMultiObjectiveBehaviour> unranked = new List<IMultiObjectiveBehaviour>(_population);

            int rank = 1;

            // All behaviours must be ranked
            while (unranked.Count > 0)
            {
                int count = unranked.Count;

                // Iterate in reverse to keep indices correct
                for (int i = count - 1; i >= 0; i--)
                {
                    IMultiObjectiveBehaviour b = unranked[i];

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

                // Increment rank
                rank++;
            }

            _maxRank = rank - 1;
        }
    }
}