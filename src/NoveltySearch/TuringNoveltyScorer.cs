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
    class TuringNoveltyScorer<TGenome> : INoveltyScorer<TGenome> where TGenome : IGenome<TGenome>
    {
        private static readonly ILog _logger = LogManager.GetLogger("Novelty Search");

        private readonly LimitedQueue<FitnessInfo> _archive;

        private readonly NoveltySearchParameters _params;

        private double _pMin;
        private int _generationsSinceArchiveAddition = -1;

        private readonly int _reportInterval;
        private int _generation;
        private long _knnTotalTimeSpent;
        private int _belowMinimumCriteria;

        public TuringNoveltyScorer(NoveltySearchParameters parameters)
        {
            _params = parameters;
            _archive = new LimitedQueue<FitnessInfo>(_params.ArchiveLimit);

            _pMin = _params.PMin;
            _generation = 0;
            _reportInterval = _params.ReportInterval;
            _knnTotalTimeSpent = 0;
        }

        public void Score(IDictionary<TGenome, FitnessInfo> behaviours)
        {
            _generation++;
            List<FitnessInfo> combinedBehaviours = new List<FitnessInfo>(behaviours.Values);
            combinedBehaviours.AddRange(_archive.ToList());

            Knn knn = new Knn(_params.K);

            knn.Initialize(combinedBehaviours);

            _knnTotalTimeSpent += knn.TimeSpent;

            int added = 0;

            foreach (TGenome genome in behaviours.Keys)
            {
                FitnessInfo behaviour = behaviours[genome];

                double score;

                double redundantTimeSteps = behaviour._auxFitnessArr[0]._value;

                // Check if behaviour meets minimum criteria
                if (redundantTimeSteps / (behaviour._auxFitnessArr.Length - 1) >= _params.MinimumCriteriaReadWriteLowerThreshold)
                {
                    score = knn.AverageDistToKnn(behaviour);
                }
                else
                {
                    // Minimum criteria is not met.
                    score = 0d;
                    _belowMinimumCriteria++;
                }

                genome.EvaluationInfo.SetFitness(score);

                if (score > _pMin)
                {
                    _archive.Enqueue(behaviour);
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
                }
            }

            if (_generation % _reportInterval == 0)
            {
                _logger.Info($"Archive size: {_archive.Count}, pMin: {_pMin.ToString("0.00")}. Avg knn time spent/gen: {_knnTotalTimeSpent / _reportInterval} ms." + $"Average individuals/gen below minimum criteria: {(float) _belowMinimumCriteria / (float) _reportInterval}");
                _knnTotalTimeSpent = 0;
                _belowMinimumCriteria = 0;
            }
        }
    }
}
