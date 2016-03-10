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

            // Compute averages
            // 
            double[] totals = new double[combinedBehaviours[0]._auxFitnessArr.Length - 1];

            for (int i = 0; i < combinedBehaviours.Count; i++)
            {
                FitnessInfo behaviour = combinedBehaviours[i];
                for (int j = 1; j < totals.Length; j++)
                {
                    totals[j-1] += behaviour._auxFitnessArr[j]._value;
                }
            }

            double[] avgs = new double[totals.Length];
            for (int j = 0; j < totals.Length; j++)
            {
                avgs[j] = totals[j] / combinedBehaviours.Count;
            }

            Debug.DLog("Averages: " + Utilities.ToString(avgs));

            // Calculate distance from average
            foreach (TGenome genome in behaviours.Keys)
            {
                FitnessInfo behaviour = behaviours[genome];

                double result = 0f;
                for (int i = 0; i < avgs.Length; i++)
                {
                    result += Math.Abs(avgs[i] - behaviour._auxFitnessArr[i]._value);
                }

                result /= behaviour._auxFitnessArr.Length;
                result *= behaviour._auxFitnessArr[0]._value;

                if (result >= _params.PMin)
                {
                    _archive.Enqueue(behaviour);
                }

                genome.EvaluationInfo.SetFitness(result);
            }
        }
    }
}
