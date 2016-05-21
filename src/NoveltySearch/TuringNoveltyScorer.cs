using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ENTM.Base;
using ENTM.Distance;
using ENTM.MultiObjective;
using log4net;
using SharpNeat.Core;
using SharpNeat.Utility;

namespace ENTM.NoveltySearch
{
    class TuringNoveltyScorer<TGenome> : INoveltyScorer<TGenome> where TGenome : class, IGenome<TGenome>
    {
        private static readonly ILog _logger = LogManager.GetLogger("Novelty Search");

        private readonly NoveltySearchParameters _params;
        private readonly LimitedQueue<Behaviour<TGenome>> _archive;

        private readonly Stopwatch _timer = new Stopwatch();
        public long TimeSpent => _timer.ElapsedMilliseconds;

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
        private bool _pMinLowerThresholdReached;
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

        public void Score(IList<Behaviour<TGenome>> behaviours, int noveltyObjective)
        {
            _timer.Restart();

            _generation++;

            // Set of combined behaviours. Because of elitism, the same behaviour could appear twice
            HashSet<Behaviour<TGenome>> combinedBehaviours = new HashSet<Behaviour<TGenome>>(behaviours);

            if (_params.EnableArchiveComparison)
            {
                combinedBehaviours.UnionWith(_archive);
            }

            int dims = behaviours[0].Evaluation.NoveltyVectors[0].Length;
            KnnMultiDimensional knnMultiDimensional = KnnMultiDimensional.Create(combinedBehaviours.ToArray(), dims);

            switch (_params.VectorMode)
            {
                case NoveltyVectorMode.WritePattern:
                    // Unknown boundaries
                    break;

                case NoveltyVectorMode.ReadContent:
                    for (int i = 0; i < dims; i++)
                    {
                        // Content is between 0 and 1
                        knnMultiDimensional.SetVectorBoundaries(i, 0d, 1d);
                    }

                    break;
                case NoveltyVectorMode.WritePatternAndInterp:

                    // Interp is 0-1, write pattern unknown
                    knnMultiDimensional.SetVectorBoundaries(1, 0d, 1d);

                    break;

                case NoveltyVectorMode.ShiftJumpInterp:
                    // Shift is -1, 0 or 1
                    knnMultiDimensional.SetVectorBoundaries(1, -1d, 1d);
                    knnMultiDimensional.SetVectorBoundaries(1, 0d, 1d);
                    knnMultiDimensional.SetVectorBoundaries(1, 0d, 1d);

                    break;

                case NoveltyVectorMode.EnvironmentAction:
                    for (int i = 0; i < dims; i++)
                    {
                        // action is between 0 and 1
                        knnMultiDimensional.SetVectorBoundaries(i, 0d, 1d);
                    }
                    break;

                default:
                    _logger.Warn($"Unimplemented NoveltyVectorMode {_params.VectorMode}");
                    break;
            }

            knnMultiDimensional.Initialize();

            _knnTotalTimeSpent += knnMultiDimensional.TimeSpent;

            int added = 0;

            foreach (Behaviour<TGenome> b in behaviours)
            {
                double objectiveScore = b.Evaluation.ObjectiveFitness;

                if (objectiveScore >= _maxObjectiveScore)
                {
                    _maxObjectiveScore = objectiveScore;
                }

                // Check if behaviour meets minimum criteria
                if (MeetsMinimumCriteria(b))
                {
                    double score = knnMultiDimensional.AverageDistToKnn(b, _params.K);

                    if (_params.ObjectiveFactorExponent > 0)
                    {
                        score *= Math.Pow(objectiveScore, _params.ObjectiveFactorExponent);
                    }

                    // Novelty score objective
                    b.Objectives[noveltyObjective] = score;

                    if (score > _pMin)
                    {
                        _archive.Enqueue(b);
                        added++;
                    }
                }
                else
                {
                    // Minimum criteria is not met.
                    _belowMinimumCriteria++;
                    b.NonViable = true;
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
                _logger.Info($"Archive size: {_archive.Count}, pMin: {_pMin:F2}. Avg knn time spent/gen: {_knnTotalTimeSpent/_reportInterval} ms. " + $"Average individuals/gen below minimum criteria: {(float) _belowMinimumCriteria/(float) _reportInterval}, Max Objective score: {_maxObjectiveScore:F4}");
                _knnTotalTimeSpent = 0;
                _belowMinimumCriteria = 0;
            }

            _timer.Stop();
        }

        private bool MeetsMinimumCriteria(Behaviour<TGenome> b)
        {
            if (_params.VectorMode == NoveltyVectorMode.EnvironmentAction) return true;

            double redundantTimeSteps = b.Evaluation.MinimumCriteria[0];
            double totalTimeSteps = b.Evaluation.MinimumCriteria[1];

            return redundantTimeSteps / totalTimeSteps <= _params.MinimumCriteriaReadWriteLowerThreshold;
        }
    }
}
