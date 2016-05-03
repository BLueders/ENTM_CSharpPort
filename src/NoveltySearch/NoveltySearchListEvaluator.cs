using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Threading.Tasks;
using ENTM.MultiObjective;
using ENTM.Utility;
using log4net;
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
    public class NoveltySearchListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome> 
        where TPhenome : class
    {
        private static readonly ILog _logger = LogManager.GetLogger("List Evaluator");

        private const int OBJ_COUNT = 3;

        private const int OBJ_OBJECTIVE_FITNESS = 0;
        private const int OBJ_NOVELTY_SCORE = 1;
        private const int OBJ_GENOMIC_DIVERSITY = 2;


        readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        readonly IPhenomeEvaluator<TPhenome> _phenomeEvaluator;
        readonly INoveltyScorer<TGenome> _noveltyScorer; 
        readonly ParallelOptions _parallelOptions;
        readonly EvaluationMethod _evalMethod;
        
        delegate IList<Behaviour<TGenome>> EvaluationMethod(IList<TGenome> genomeList);

        private bool _noveltySearchEnabled;
        private bool _multiobjectiveEnabled;
        private bool _reevaluateOnce;
        private int _generation;

        /// <summary>
        /// Determines if the population is scored by novelty search or regular environmental fitness.
        /// </summary>
        public bool NoveltySearchEnabled
        {
            get { return _noveltySearchEnabled; }

            set
            {
                _noveltySearchEnabled = value;

                // When we switch between evaluation modes we must always reevaluate the population
                _reevaluateOnce = true;
            }
        }

        public bool MultiObjectiveEnabled
        {
            get { return _multiobjectiveEnabled; }
            set { _multiobjectiveEnabled = value; }
        }


        public List<TGenome> Archive => new List<TGenome>(_noveltyScorer.Archive);

        /// <summary>
        /// Returns true if the novelty search has been completed, and the evaluation should switch to objective search.
        /// </summary>
        public bool NoveltySearchComplete => _noveltyScorer.StopConditionSatisfied;

        #region Constructors
        /// <summary>
        /// Construct with the provided IGenomeDecoder, IPhenomeEvaluator, enableMultiThreading flag, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        public NoveltySearchListEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
                                           IPhenomeEvaluator<TPhenome> phenomeEvaluator,
                                           INoveltyScorer<TGenome> noveltyScorer,
                                           bool enableMultiThreading,
                                           ParallelOptions options)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _noveltyScorer = noveltyScorer;
            _parallelOptions = options;
            _generation = 0;

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
            _generation++;
            IList<TGenome> filteredList;

            // If we are using novelty search we must reevaluate all genomes, because a given score can change in between 
            // generations for the same behaviour
            if (NoveltySearchEnabled || _reevaluateOnce)
            {
                _reevaluateOnce = false;
                filteredList = genomeList;
            }
            else
            {
                filteredList = new List<TGenome>(genomeList.Count);
                foreach (TGenome genome in genomeList)
                {
                    // Only evalutate new genomes
                    if (!genome.EvaluationInfo.IsEvaluated)
                    {   // Add the genome to the temp list for evaluation later.
                        filteredList.Add(genome);
                    }
                    else
                    {   // Register that the genome skipped an evaluation.
                        genome.EvaluationInfo.EvaluationPassCount++;
                    }
                }
            }
          
            // We save the fitness info in a dictionary, since we can't apply the scores directly, because the entire generation
            // must be evaluated to calculate novelty score
            IList<Behaviour<TGenome>> behaviours = _evalMethod(filteredList);

            if (!NoveltySearchEnabled && !MultiObjectiveEnabled)
            {
                // Ignore novelty score and multiobjective, apply environmental objective fitness
                ApplyEnvironmentScoresOnly(behaviours);
            }
            else
            {
                if (NoveltySearchEnabled)
                {
                    // Apply the novelty score
                    _noveltyScorer.Score(behaviours, OBJ_NOVELTY_SCORE);
                }

                if (MultiObjectiveEnabled)
                {
                    
                }
            }
        }

        /// <summary>
        /// Apply the environment objective scores to the genomes, ignoring novelty search.
        /// </summary>
        /// <param name="fitness"></param>
        private void ApplyEnvironmentScoresOnly(IList<Behaviour<TGenome>> fitness)
        {
            foreach (Behaviour<TGenome> b in fitness)
            {
                b.Genome.EvaluationInfo.SetFitness(b.Score._fitness);
            }
        }

        #endregion

        #region Evaluation methods

        private Behaviour<TGenome> EvaluateGenome(TGenome genome)
        {
            TPhenome phenome = (TPhenome) genome.CachedPhenome;
            if (null == phenome)
            {   // Decode the phenome and store a ref against the genome.
                phenome = _genomeDecoder.Decode(genome);
                genome.CachedPhenome = phenome;
            }

            if (null == phenome)
            {   // Non-viable genome.
                return default(Behaviour<TGenome>);
            }

            // Evaluate the genome!
            FitnessInfo score = _phenomeEvaluator.Evaluate(phenome);

            Behaviour<TGenome> behaviour = new Behaviour<TGenome>(genome, score, OBJ_COUNT);

            // First Objetive - Objective fitness
            behaviour.Objectives[OBJ_OBJECTIVE_FITNESS] = score._fitness;

            return behaviour;
        }

        /// <summary>
        /// Evaluate on multiple threads, according to the parallel options
        /// </summary>
        /// <param name="genomeList"></param>
        private IList<Behaviour<TGenome>> EvaluateParallel(IList<TGenome> genomeList)
        {
            int concurrencyLevel = _parallelOptions.MaxDegreeOfParallelism == -1
                ? Environment.ProcessorCount
                : Math.Min(Environment.ProcessorCount, _parallelOptions.MaxDegreeOfParallelism);

            if (_generation == 1) _logger.Info($"Running parallel, number of threads: {concurrencyLevel}");

            ConcurrentAddList<Behaviour<TGenome>> behaviours = new ConcurrentAddList<Behaviour<TGenome>>();

            Parallel.ForEach(genomeList, _parallelOptions, genome =>
            {
                behaviours.Add(EvaluateGenome(genome));
            });

            return behaviours.List;
        }

        /// <summary>
        /// Evaluate on a single thread
        /// </summary>
        /// <param name="genomeList"></param>
        private IList<Behaviour<TGenome>> EvaluateSerial(IList<TGenome> genomeList) 
        {
            if (_generation == 1) _logger.Info($"Running serial");

            IList<Behaviour<TGenome>> behaviours = new List<Behaviour<TGenome>>();
            foreach (TGenome genome in genomeList)
            {
                behaviours.Add(EvaluateGenome(genome));
            }

            return behaviours;
        }

        #endregion
    }
}
