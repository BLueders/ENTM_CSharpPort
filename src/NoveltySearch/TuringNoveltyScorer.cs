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
            Stopwatch timer = new Stopwatch();
            timer.Start();

            _generation++;
            List<FitnessInfo> combinedBehaviours = new List<FitnessInfo>(behaviours.Values);
            combinedBehaviours.AddRange(_archive.ToList());

            Console.WriteLine($"Preliminary: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            Knn knn = new Knn(_params.K);

            knn.Initialize(combinedBehaviours);

            Console.WriteLine($"KNN: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            _knnTotalTimeSpent += knn.TimeSpent;

            int added = 0;

            foreach (TGenome genome in behaviours.Keys)
            {
                FitnessInfo behaviour = behaviours[genome];
                double score = knn.AverageDistToKnn(behaviour);
                
                genome.EvaluationInfo.SetFitness(score);

                if (score > _pMin)
                {
                    _archive.Enqueue(behaviour);
                    added++;
                }
            }

            Console.WriteLine($"Scoring: {timer.ElapsedMilliseconds} ms");
            timer.Restart();

            if (added > 0)
            {
                _generationsSinceArchiveAddition = 0;
                
                // If more than the specified amount of behaviours are added to the archive, adjust PMin up
                if (added > _params.AdditionsPMinAdjustUp)
                {
                    _pMin *= _params.PMinAdjustUp;
                    _logger.Info($"PMin adjusted up to {_pMin}");
                }
            }
            else
            {
                _generationsSinceArchiveAddition++;

                // If more than the specified amount of generations has passed without archive additions, adjust PMin down
                if (_generationsSinceArchiveAddition > _params.GenerationsPMinAdjustDown)
                {
                    _pMin *= _params.PMinAdjustDown;

                    _logger.Info($"PMin adjusted down to {_pMin}");
                }
            }

            if (_generation % _reportInterval == 0)
            {
                _logger.Info($"Archive size: {_archive.Count}, pMin: {_pMin}, Avg knn time spent: {_knnTotalTimeSpent / _reportInterval} ms");
                _knnTotalTimeSpent = 0;
            }

            Console.WriteLine($"Remaining: {timer.ElapsedMilliseconds} ms");
            timer.Stop();
        }
    }
}
