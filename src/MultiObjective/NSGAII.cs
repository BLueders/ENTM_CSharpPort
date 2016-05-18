using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{

    public class NSGAII : IMultiObjectiveScorer
    {
        private readonly ILog _logger = LogManager.GetLogger("NSGAII");

        private readonly Stopwatch _timer = new Stopwatch();
        public long TimeSpent => _timer.ElapsedMilliseconds;
        public int ParetoOptimal { get; private set; }

        private readonly ObjectiveComparer _objectiveComparer = new ObjectiveComparer();
        private readonly PopulationComparer _populationComparer = new PopulationComparer();

        private List<IMultiObjectiveBehaviour> _population;
        private int _count;
        private int _objectives;

        public void Score(IList<IMultiObjectiveBehaviour> behaviours)
        {
            _timer.Restart();

            _population = new List<IMultiObjectiveBehaviour>(behaviours);
            _count = _population.Count;
            _objectives = _population[0].Objectives.Length;

            UpdateDominations();
            RankPopulation();

            // Sort population based on rank and crowding distance
            _population.Sort(_populationComparer);

            for (int i = 0; i < _count; i++)
            {
                IMultiObjectiveBehaviour b = _population[i];

                // Score the individuals
                b.MultiObjectiveScore = (double) i / (_count - 1);

                // Reset the behaviour to remove any references, since the object might be cached in the novelty archive
                b.Reset();
            }

            _timer.Stop();
        }


        /// <summary>
        /// Compares all behaviours in the population to each other, to determine who dominates who
        /// </summary>
        private void UpdateDominations()
        {
            // Compare all bahaviours to each other
            for (int x = 0; x < _count; x++)
            {
                IMultiObjectiveBehaviour bx = _population[x];

                for (int y = x + 1; y < _count; y++)
                {
                    IMultiObjectiveBehaviour by = _population[y];

                    if (Dominates(bx, by))
                    {
                        bx.Dominates.Add(by);
                        by.DominatedCount++;
                    }
                    else if (Dominates(by, bx))
                    {
                        by.Dominates.Add(bx);
                        bx.DominatedCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Check if behaviour x dominates behaviour y
        /// </summary>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        private bool Dominates(IMultiObjectiveBehaviour bx, IMultiObjectiveBehaviour by)
        {
            bool dominates = false;

            // Compare all objectives
            for (int i = 0; i < _objectives; i++)
            {
                double ox = bx.Objectives[i];
                double oy = by.Objectives[i];

                if (ox < oy)
                {
                    // Objective i is worse, bx does not dominate by. 
                    // No need to check remaining objectives, so bail out here
                    return false;
                }
                if (ox > oy)
                {
                    // Objective i is better, potential domination, but we need to check remaining objectives
                    dominates = true;
                }
            }

            return dominates;
        }

        /// <summary>
        /// Distributes all behaviours into their respective ranks.
        /// Calculates Crowding distances.
        /// </summary>
        private void RankPopulation()
        {
            List<IMultiObjectiveBehaviour> unranked = new List<IMultiObjectiveBehaviour>(_population);

            int currentRank = 1;

            // All behaviours must be ranked
            while (unranked.Count > 0)
            {
                List<IMultiObjectiveBehaviour> front = new List<IMultiObjectiveBehaviour>();

                int count = unranked.Count;

                // Iterate in reverse to keep indices correct for removal
                for (int i = count - 1; i >= 0; i--)
                {
                    IMultiObjectiveBehaviour b = unranked[i];

                    if (b.DominatedCount == 0)
                    {
                        // Behaviour is non-dominated, is on the current front
                        front.Add(b);
                        unranked.RemoveAt(i);
                    }
                }

                int fCount = front.Count;
                if (currentRank == 1) ParetoOptimal = fCount;

                for (int i = 0; i < fCount; i++)
                {
                    IMultiObjectiveBehaviour b = front[i];

                    // Individual is on the current front, rank it accordingly
                    b.Rank = currentRank;

                    int dCount = b.Dominates.Count;
                    for (int j = 0; j < dCount; j++)
                    {
                        // Decrement dominated count for all behaviours dominated by current front behaviours
                        b.Dominates[j].DominatedCount--;
                    }
                }

                // Calculate crowding distances for front behaviours
                CrowdingDistance(front.ToArray());

                // Increment rank
                currentRank++;
            }
        }

        /// <summary>
        /// Calculate the crowding distance for each behaviour in a non-dominated set (front)
        /// </summary>
        /// <param name="front"></param>
        public void CrowdingDistance(IMultiObjectiveBehaviour[] front)
        {
            int fCount = front.Length;

            if (fCount == 1) front[0].CrowdingDistance = 1d;

            for (int i = 0; i < _objectives; i++)
            {
                // Sort the population based on each objective
                _objectiveComparer.Objective = i;
                Array.Sort(front, _objectiveComparer);

                IMultiObjectiveBehaviour bMin = front[0];
                IMultiObjectiveBehaviour bMax = front[fCount - 1];

                // Max - min = span of objective
                double oSpan = bMax.Objectives[i] - bMin.Objectives[i];

                bMin.CrowdingDistance = 1d;
                bMax.CrowdingDistance = 1d;

                for (int j = 1; j < fCount - 1; j++)
                {
                    IMultiObjectiveBehaviour b = front[j];


                    if (b.CrowdingDistance == 1d || oSpan == 0d) continue;

                    b.CrowdingDistance += (front[j + 1].Objectives[i] - front[j - 1].Objectives[i]) / oSpan;
                }
            }
        }
    }
}