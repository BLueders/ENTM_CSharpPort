using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments;
using ENTM.Utility;
using log4net;
using SharpNeat.Core;
using SharpNeat.Utility;
using Utilities = ENTM.Utility.Utilities;

namespace ENTM.NoveltySearch
{
    class TuringNoveltyScorer<TGenome> : INoveltyScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        private static readonly ILog _logger = LogManager.GetLogger("Novelty Search");

        private readonly NoveltySearchParameters _params;
        private readonly LimitedQueue<Behaviour<TGenome>> _archive;

        public IList<TGenome> Archive
        {
            get
            {
                IList<TGenome> genomes = new List<TGenome>();
                foreach (Behaviour<TGenome> b in _archive)
                {
                    genomes.Add(b.Genome);
                }

                return genomes;
            }
        }

        public bool StopConditionSatisfied => 
            _maxObjectiveScore >= _params.ObjectiveScoreThreshold ||
            _pMinLowerThresholdReached || 
            _generation >= _params.MaxNoveltySearchGenerations;

        private double _pMin;
        private int _generationsSinceArchiveAddition = -1;

        private readonly int _reportInterval;
        private int _generation;
        private long _knnTotalTimeSpent;
        private int _belowMinimumCriteria;
        private bool _pMinLowerThresholdReached = false;
        private double _maxObjectiveScore = 0d;

        public TuringNoveltyScorer(NoveltySearchParameters parameters)
        {
            _params = parameters;
            _archive = new LimitedQueue<Behaviour<TGenome>>(_params.ArchiveLimit);

            _pMin = _params.PMin;
            _generation = 0;
            _reportInterval = _params.ReportInterval;
            _knnTotalTimeSpent = 0;
        }

        public void Score(IList<Behaviour<TGenome>> behaviours)
        {
            _generation++;
            List<Behaviour<TGenome>> combinedBehaviours = new List<Behaviour<TGenome>>(_archive);
            combinedBehaviours.AddRange(behaviours);

            Knn<TGenome> knn = new Knn<TGenome>(_params.K);

            // Start index 1, since we use the 1 position in the array to store the minimum criteria.
            knn.Initialize(combinedBehaviours, 1);

            _knnTotalTimeSpent += knn.TimeSpent;

            int added = 0;

            foreach (Behaviour<TGenome> b in behaviours)
            {
                FitnessInfo behaviour = b.Score;

                if (behaviour._fitness >= _maxObjectiveScore)
                {
                    _maxObjectiveScore = behaviour._fitness;
                }

                double score;

                double redundantTimeSteps = behaviour._auxFitnessArr[0]._value;

                // Check if behaviour meets minimum criteria
                if (redundantTimeSteps / (behaviour._auxFitnessArr.Length - 1) <= _params.MinimumCriteriaReadWriteLowerThreshold)
                {
                    score = knn.AverageDistToKnn(b);

                    if (_params.ObjectiveFactorExponent > 0)
                    {
                        score *= Math.Pow(behaviour._fitness, _params.ObjectiveFactorExponent);
                    }
                }
                else
                {
                    // Minimum criteria is not met.
                    score = 0d;
                    _belowMinimumCriteria++;
                }

                b.Genome.EvaluationInfo.SetFitness(score);
                
                if (score > _pMin)
                {
                    _archive.Enqueue(b);
                    added++;
                }
            }

            if (added > 0)
            {
                _generationsSinceArchiveAddition = 0;
                
                // If more than the specified amount of behaviours are added to the archive, adjust PMin up
                if (added > _params.AdditionsPMinAdjustUp)
                {
                    _pMin *= _params.PMinAdjustUp;
                    _logger.Info($"PMin adjusted up to {_pMin.ToString("0.00")}");
                }
            }
            else
            {
                _generationsSinceArchiveAddition++;

                // If more than the specified amount of generations has passed without archive additions, adjust PMin down
                if (_generationsSinceArchiveAddition > _params.GenerationsPMinAdjustDown)
                {
                    _pMin *= _params.PMinAdjustDown;

                    _logger.Info($"PMin adjusted down to {_pMin.ToString("0.00")}");

                    // If the minimum threshold for pMin is reached, we can assume that the behaviour space has been exhausted, or that the algorithm
                    // can not find any new novel behaviours for other reasons.
                    if (_pMin < _params.PMinLowerThreshold)
                    {
                        _logger.Info($"PMin lower threshold reached.");
                        _pMinLowerThresholdReached = true;
                    }
                }
            }

            if (_generation % _reportInterval == 0)
            {
                _logger.Info($"Archive size: {_archive.Count}, pMin: {_pMin:F2}. Avg knn time spent/gen: {_knnTotalTimeSpent / _reportInterval} ms. " + $"Average individuals/gen below minimum criteria: {(float) _belowMinimumCriteria / (float) _reportInterval}, Max Objective Score: {_maxObjectiveScore:F4}");
                _knnTotalTimeSpent = 0;
                _belowMinimumCriteria = 0;
            }
        }

    }
}
