using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ENTM.Utility;
using SharpNeat.Core;

namespace ENTM.NoveltySearch
{
    /// <summary>
    /// Implementation of IGenomeListEvaluator.
    /// Provides a novelty search evaluation dependant on the entire generation under evaluation
    /// Provides parallel and serial evaluation for debugging purposes. 
    /// Phenome caching is enabled.
    /// </summary>
    /// <typeparam name="TGenome"></typeparam>
    /// <typeparam name="TPhenome"></typeparam>
    class NoveltySearchListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome> 
        where TPhenome : class
    {
        readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        readonly IPhenomeEvaluator<TPhenome> _phenomeEvaluator;
        readonly ParallelOptions _parallelOptions;
        readonly EvaluationMethod _evalMethod;

        delegate IDictionary<TGenome, FitnessInfo> EvaluationMethod(IList<TGenome> genomeList);

        /// <summary>
        /// Determines if the population is scored by novelty search or regular environmental fitness.
        /// </summary>
        public bool NoveltySearchEnabled { get; set; }

        /// <summary>
        /// The fraction of the final score that will be from novelty score.
        /// If set to 1, only novelty score will be used, if set to 0, only environmental fitness will be used.
        /// </summary>
        public double NoveltyScoreFraction { get; set; }

        #region Constructors
        /// <summary>
        /// Construct with the provided IGenomeDecoder, IPhenomeEvaluator, enableMultiThreading flag, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        public NoveltySearchListEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
                                           IPhenomeEvaluator<TPhenome> phenomeEvaluator,
                                           bool enableMultiThreading,
                                           ParallelOptions options)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _parallelOptions = options;

            // Determine the appropriate evaluation method.
            if (enableMultiThreading)   _evalMethod = EvaluateParallel;
            else                        _evalMethod = EvaluateSerial;
            
        }

        #endregion
        #region IGenomeListEvaluator<TGenome> Members

        /// <summary>
        /// Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        public ulong EvaluationCount => _phenomeEvaluator.EvaluationCount;

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the the evolutionary algorithm/search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied => _phenomeEvaluator.StopConditionSatisfied;

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            _phenomeEvaluator.Reset();
        }

        /// <summary>
        /// Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        /// and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        public void Evaluate(IList<TGenome> genomeList)
        {
            IDictionary<TGenome, FitnessInfo> fitness = _evalMethod(genomeList);
            CalculateNoveltyScores(fitness);
        }

        #endregion

        #region Evaluation methods

        private FitnessInfo EvaluateGenome(TGenome genome)
        {
            TPhenome phenome = (TPhenome) genome.CachedPhenome;
            if (null == phenome)
            {   // Decode the phenome and store a ref against the genome.
                phenome = _genomeDecoder.Decode(genome);
                genome.CachedPhenome = phenome;
            }

            if (null == phenome)
            {   // Non-viable genome.
                return FitnessInfo.Zero;
            }
            else
            {
                return _phenomeEvaluator.Evaluate(phenome);
            }
        }

        private void CalculateNoveltyScores(IDictionary<TGenome, FitnessInfo> fitness)
        {
            // Compute averages
            double[] totals = null;
            foreach (TGenome genome in fitness.Keys)
            {
                FitnessInfo scores = fitness[genome];
                if (totals == null) totals = new double[scores._auxFitnessArr.Length];

                for (int i = 0; i < totals.Length; i++)
                {
                    totals[i] += scores._auxFitnessArr[i]._value;
                }
            }

            double[] avgs = new double[totals.Length];
            for (int j = 0; j < totals.Length; j++)
            {
                avgs[j] = totals[j] / fitness.Count;
            }

            Debug.DLog("Averages: " + Utilities.ToString(avgs));

            // Calculate distance from average
            foreach (TGenome genome in fitness.Keys)
            {
                FitnessInfo scores = fitness[genome];

                double result = 0f;
                for (int i = 0; i < avgs.Length; i++)
                {
                    result += Math.Abs(avgs[i] - scores._auxFitnessArr[i]._value);
                }

                //genome.EvaluationInfo.SetFitness(result);
                genome.EvaluationInfo.SetFitness(scores._fitness);
            }
        }

        /// <summary>
        /// Evaluate on multiple threads, according to the parallel options
        /// </summary>
        /// <param name="genomeList"></param>
        private IDictionary<TGenome, FitnessInfo> EvaluateParallel(IList<TGenome> genomeList)
        {
            ConcurrentDictionary<TGenome, FitnessInfo> fitness = new ConcurrentDictionary<TGenome, FitnessInfo>();
            Parallel.ForEach(genomeList, _parallelOptions, genome =>
            {
                FitnessInfo scores = EvaluateGenome(genome);
                fitness.TryAdd(genome, scores);
            });
            return fitness;
        }

        /// <summary>
        /// Evaluate on a single thread
        /// </summary>
        /// <param name="genomeList"></param>
        private IDictionary<TGenome, FitnessInfo> EvaluateSerial(IList<TGenome> genomeList) 
        {
            Dictionary<TGenome, FitnessInfo> fitness = new Dictionary<TGenome, FitnessInfo>();
            foreach (TGenome genome in genomeList)
            {
                FitnessInfo scores = EvaluateGenome(genome);
                fitness.Add(genome, scores);
            }
            return fitness;
        }

        #endregion
    }
}
