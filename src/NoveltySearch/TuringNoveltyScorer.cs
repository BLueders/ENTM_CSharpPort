using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Utility;
using SharpNeat.Core;
using SharpNeat.Utility;
using Utilities = ENTM.Utility.Utilities;

namespace ENTM.NoveltySearch
{
    class TuringNoveltyScorer<TGenome> : INoveltyScorer<TGenome> where TGenome : IGenome<TGenome>
    {
        private readonly LimitedQueue<FitnessInfo> _archive;

        private readonly NoveltySearchParameters _params;

        public TuringNoveltyScorer(NoveltySearchParameters parameters)
        {
            _params = parameters;
            _archive = new LimitedQueue<FitnessInfo>(_params.ArchiveLimit);
        }

        public void Score(IDictionary<TGenome, FitnessInfo> behaviours)
        {

            List<FitnessInfo> combinedBehaviours = new List<FitnessInfo>(behaviours.Values);
            combinedBehaviours.AddRange(_archive.ToList());

            Knn knn = new Knn(5);

            knn.Initialize(combinedBehaviours);

            foreach (TGenome genome in behaviours.Keys)
            {
                FitnessInfo behaviour = behaviours[genome];
                double score = knn.AverageDistToKnn(behaviour);
                
                genome.EvaluationInfo.SetFitness(score);

                if (score > _params.PMin)
                {
                    _archive.Enqueue(behaviour);
                }
            }
        }
    }
}
