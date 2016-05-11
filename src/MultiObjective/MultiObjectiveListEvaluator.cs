﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Base;
using ENTM.NoveltySearch;
using ENTM.Utility;
using log4net;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{

    /// <summary>
    /// Implementation of IGenomeListEvaluator.
    /// Provides a novelty search evaluation dependant on the entire generation under evaluation
    /// Provides parallel and serial evaluation for debugging purposes. 
    /// Phenome caching is enabled.
    /// </summary>
    /// <typeparam name="TGenome"></typeparam>
    /// <typeparam name="TPhenome"></typeparam>
    public class MultiObjectiveListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome> 
        where TPhenome : class
    {
        private static readonly ILog _logger = LogManager.GetLogger("List Evaluator");

        private const int OBJ_COUNT = 3;

        private const int OBJ_OBJECTIVE_FITNESS = 0;
        private const int OBJ_NOVELTY_SCORE = 1;
        private const int OBJ_GENETIC_DIVERSITY = 2;

        public int ReportInterval { get; set; } = 0;

        private readonly Stopwatch _timer = new Stopwatch();

        private double[] _maxObjectiveScores;
        private long _timeSpentEvaluation, _timeSpentNoveltySearch, _timeSpentGeneticDiversity, _timeSpentMultiObjective;

        private MultiObjectiveParameters _multiObjectiveParameters;
        public MultiObjectiveParameters MultiObjectiveParams
        {
            get { return _multiObjectiveParameters; }
            set
            {
                _multiObjectiveParameters = value;
                _geneticDiversityScorer.Params = value;
            }
        }

        delegate IList<Behaviour<TGenome>> EvaluationMethod(IList<TGenome> genomeList);

        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly IMultiObjectiveEvaluator<TPhenome> _phenomeEvaluator;
        private readonly INoveltyScorer<TGenome> _noveltyScorer;
        private readonly IGeneticDiversityScorer<TGenome> _geneticDiversityScorer;
        private readonly IMultiObjectiveScorer _multiObjectiveScorer;
        private readonly ParallelOptions _parallelOptions;
        private readonly EvaluationMethod _evalMethod;


        private bool _multiObjectiveEnabled;
        private bool _multiObjectiveComplete;
        private bool _noveltySearchEnabled;

        private int _generation;
        private bool _reevaluateOnce;

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
            get { return _multiObjectiveEnabled; }
            set { _multiObjectiveEnabled = value; }
        }


        public List<TGenome> Archive => new List<TGenome>(_noveltyScorer.Archive);

        /// <summary>
        /// Returns true if the novelty search has been completed, and the evaluation should switch to objective search.
        /// </summary>
        public bool NoveltySearchComplete => _noveltyScorer.StopConditionSatisfied;

        #region Constructors
        public MultiObjectiveListEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
                                           IMultiObjectiveEvaluator<TPhenome> phenomeEvaluator,
                                           INoveltyScorer<TGenome> noveltyScorer,
                                           IGeneticDiversityScorer<TGenome> geneticDiversityScorer,
                                           IMultiObjectiveScorer multiObjectiveScorer,
                                           bool enableMultiThreading,
                                           ParallelOptions options)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _noveltyScorer = noveltyScorer;
            _geneticDiversityScorer = geneticDiversityScorer;
            _multiObjectiveScorer = multiObjectiveScorer;
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
                    // Only evalutate new genomes. Elite genomes will be skipped, as they already have an assigned fitness.
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

            _timer.Restart();

            IList<Behaviour<TGenome>> behaviours = _evalMethod(filteredList);

            _timer.Stop();
            _timeSpentEvaluation += _timer.ElapsedMilliseconds;

            if (!NoveltySearchEnabled && !MultiObjectiveEnabled)
            {
                // Ignore novelty score and multiobjective, apply environmental objective fitness
                for (int i = 0; i < behaviours.Count; i++)
                {
                    behaviours[i].ApplyObjectiveFitnessOnly();
                }
            }
            else
            {
                if (NoveltySearchEnabled)
                {
                    // Calculate novelty scores
                    _noveltyScorer.Score(behaviours, OBJ_NOVELTY_SCORE);
                    _timeSpentNoveltySearch += _noveltyScorer.TimeSpent;
                }

                if (MultiObjectiveEnabled)
                {
                    Behaviour<TGenome>[] viable = behaviours.Where(x => !x.NonViable).ToArray();

                    // Score genetic diversity, since speciation will not work with MO
                    _geneticDiversityScorer.Score(viable, OBJ_GENETIC_DIVERSITY);
                    _timeSpentGeneticDiversity += _geneticDiversityScorer.TimeSpent;

                    // Apply multi objective
                    _multiObjectiveScorer.Score(viable);
                    _timeSpentMultiObjective += _multiObjectiveScorer.TimeSpent;

                    _maxObjectiveScores = new double[OBJ_COUNT];

                    int vCount = viable.Length;
                    for (int i = 0; i < vCount; i++)
                    {
                        Behaviour<TGenome> b = behaviours[i];
                        for (int j = 0; j < OBJ_COUNT; j++)
                        {
                            // Debug max objective scores
                            if (b.Objectives[j] > _maxObjectiveScores[j])
                            {
                                _maxObjectiveScores[j] = b.Objectives[j];
                            }
                        }

                        // Apply final fitness as multi objective score
                        b.ApplyMultiObjectiveScore();
                    }
                }
                else
                {
                    // Only novelty score
                    for (int i = 0; i < behaviours.Count; i++)
                    {
                        behaviours[i].ApplyNoveltyScoreOnly();
                    }
                }
            }

            if (ReportInterval > 0 && _generation % ReportInterval == 0)
            {
                StringBuilder sb = new StringBuilder();

                if (MultiObjectiveEnabled)
                {
                    sb.Append($"Max Objective scores:");
                    for (int i = 0; i < OBJ_COUNT; i++)
                    {
                        sb.Append($" [{i}]: {_maxObjectiveScores[i]:F04}");
                    }
                }

                sb.Append($" Avg time spent: [Evaluation: {_timeSpentEvaluation / ReportInterval} ms]");

                if (NoveltySearchEnabled)
                {
                    sb.Append($" [Novelty Search: {_timeSpentNoveltySearch / ReportInterval} ms]");
                }
                if (MultiObjectiveEnabled)
                {
                    sb.Append($" [Genetic Diversity: {_timeSpentGeneticDiversity / ReportInterval} ms]");
                    sb.Append($" [Multi Objective: {_timeSpentMultiObjective / ReportInterval} ms]");
                }

                _logger.Info(sb.ToString());

                _timeSpentEvaluation = 0;
                _timeSpentNoveltySearch = 0;
                _timeSpentGeneticDiversity = 0;
                _timeSpentMultiObjective = 0;
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
            Behaviour<TGenome> behaviour = new Behaviour<TGenome>(genome, OBJ_COUNT);
            behaviour.Evaluation = _phenomeEvaluator.Evaluate(phenome);

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
