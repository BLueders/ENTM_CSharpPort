using System;
using System.Collections.Generic;
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

        private int _generation;

        public TuringNoveltyScorer(NoveltySearchParameters parameters)
        {
            _params = parameters;
            _archive = new LimitedQueue<FitnessInfo>(_params.ArchiveLimit);

            _pMin = _params.PMin;
            _generation = -1;
        }

        public void Score(IDictionary<TGenome, FitnessInfo> behaviours)
        {
            _generation++;

            List<FitnessInfo> combinedBehaviours = new List<FitnessInfo>(behaviours.Values);
            combinedBehaviours.AddRange(_archive.ToList());

            Knn knn = new Knn(5);

            knn.Initialize(combinedBehaviours);

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

            if (_generation % 10 == 0)
            {
                _logger.Info($"Archive size: {_archive.Count}, pMin: {_pMin}");
            }
        }
    }
}
