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

        public int ObjectiveCount => _objectiveScorers.Count + 1;
        public int ReportInterval { get; set; } = 0;

        private readonly Stopwatch _evaluationTimer = new Stopwatch();

        private long _timeSpentEvaluation, _timeSpentNoveltySearch, _timeSpentMultiObjective;
        private long _paretoOptimal;

        public double[] MaxObjectiveScores { get; private set; }

        private MultiObjectiveParameters _multiObjectiveParameters;
        public MultiObjectiveParameters MultiObjectiveParams
        {
            get { return _multiObjectiveParameters; }
            set
            {
                _multiObjectiveParameters = value;
                foreach (IObjectiveScorer<TGenome> scorer in _objectiveScorers) {
                    scorer.Params = value;
                }
            }
        }

        delegate IList<Behaviour<TGenome>> EvaluationMethod(IList<TGenome> genomeList);

        private readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        private readonly IMultiObjectiveEvaluator<TPhenome> _phenomeEvaluator;
        private readonly INoveltyScorer<TGenome> _noveltyScorer;
        private readonly IList<IObjectiveScorer<TGenome>> _objectiveScorers;
        private readonly IMultiObjectiveScorer _multiObjectiveScorer;
        private readonly ParallelOptions _parallelOptions;
        private readonly EvaluationMethod _evalMethod;

        private Dictionary<TGenome, Behaviour<TGenome>> _previousGeneration; 

        private bool _multiObjectiveEnabled;
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
            set
            {
                _multiObjectiveEnabled = value;
                _reevaluateOnce = true;
            }
        }

        public string ObjectiveName(int i) {
            if (i == 0) {
                return "Objective";
            }
            return _objectiveScorers[i - 1].Name;
        }

        public List<TGenome> NoveltyArchive => new List<TGenome>(_noveltyScorer.Archive);

        /// <summary>
        /// Returns true if the novelty search has been completed, and the evaluation should switch to objective search.
        /// </summary>
        public bool NoveltySearchComplete => _noveltyScorer.StopConditionSatisfied;

        #region Constructors
        public MultiObjectiveListEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
                                           IMultiObjectiveEvaluator<TPhenome> phenomeEvaluator,
                                           INoveltyScorer<TGenome> noveltyScorer,
                                           IList<IObjectiveScorer<TGenome>> objectiveScorers,
                                           IMultiObjectiveScorer multiObjectiveScorer,
                                           bool enableMultiThreading,
                                           ParallelOptions options)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _noveltyScorer = noveltyScorer;
            _objectiveScorers = objectiveScorers;
            for (int i = 0; i < _objectiveScorers.Count; i++) {
                _objectiveScorers[i].Objective = i + 1;
            }
            
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
            IList<Behaviour<TGenome>> eliteBehaviours = new List<Behaviour<TGenome>>();

            // Check if elite genomes must be reevaluated
            if (_reevaluateOnce)
            {
                _reevaluateOnce = false;
                filteredList = genomeList;
            }
            else
            {
                filteredList = new List<TGenome>(genomeList.Count);

                int count = genomeList.Count;
                for (int i = 0; i < count; i++)
                {
                    TGenome genome = genomeList[i];
                    
                    // Only evalutate new genomes. Elite genomes will be skipped, as they already have an assigned fitness.
                    if (!genome.EvaluationInfo.IsEvaluated)
                    {   // Add the genome to the filtered list for evaluation later.
                        filteredList.Add(genome);
                    }
                    else
                    {   // Register that the genome skipped an evaluation.
                        genome.EvaluationInfo.EvaluationPassCount++;

                        if (NoveltySearchEnabled || MultiObjectiveEnabled)
                        {
                            // Find the cached elite behaviours, as we need them for scoring novelty and genetic diversity
                            eliteBehaviours.Add(_previousGeneration[genome]);
                        }
                    }
                }
            }

            _evaluationTimer.Restart();

            IList<Behaviour<TGenome>> evaluated = _evalMethod(filteredList);

            _evaluationTimer.Stop();

            _timeSpentEvaluation += _evaluationTimer.ElapsedMilliseconds;

            if (!NoveltySearchEnabled && !MultiObjectiveEnabled)
            {
                // Ignore novelty score and multiobjective, apply environmental objective fitness
                for (int i = 0; i < evaluated.Count; i++)
                {
                    evaluated[i].ApplyObjectiveFitnessOnly();
                }
            }
            else
            {
                // Elite behaviours must be novelty scored again
                List<Behaviour<TGenome>> combined = new List<Behaviour<TGenome>>(eliteBehaviours);
                combined.AddRange(evaluated);
                int combinedCount = combined.Count;

                if (NoveltySearchEnabled)
                {
                    // Calculate novelty scores
                    _noveltyScorer.Score(combined);
                    _timeSpentNoveltySearch += _noveltyScorer.TimeSpent;
                }

                if (MultiObjectiveEnabled)
                {
                    // Find all viable behaviours. NonViable could mean they failed to meet minimum criteria
                    Behaviour<TGenome>[] viable = combined.Where(x => !x.NonViable).ToArray();
                    int vCount = viable.Length;

                    foreach (IObjectiveScorer<TGenome> scorer in _objectiveScorers) {
                        // Score genetic diversity
                        scorer.Score(viable);
                        scorer.TimeSpentAccumulated += scorer.TimeSpent;
                    }

                    if (vCount > 0)
                    {
                        // Apply multi objective
                        _multiObjectiveScorer.Score(viable);
                        _paretoOptimal += _multiObjectiveScorer.ParetoOptimal;
                        _timeSpentMultiObjective += _multiObjectiveScorer.TimeSpent;

                        MaxObjectiveScores = new double[ObjectiveCount];

                        for (int i = 0; i < vCount; i++)
                        {
                            Behaviour<TGenome> b = viable[i];

                            // Apply final fitness as multi objective score
                            b.ApplyMultiObjectiveScore();

                            // Debug max objective scores
                            for (int j = 0; j < ObjectiveCount; j++)
                            {
                                if (b.Objectives[j] > MaxObjectiveScores[j])
                                {
                                    MaxObjectiveScores[j] = b.Objectives[j];
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("No viable genomes in population");
                    }
                   
                }
                else
                {
                    MaxObjectiveScores = new double[ObjectiveCount];

                    // Only novelty score
                    for (int i = 0; i < combinedCount; i++)
                    {
                        Behaviour<TGenome> b = combined[i];

                        b.ApplyNoveltyScoreOnly();

                        // Debug max objective scores
                        if (b.Evaluation.ObjectiveFitness > MaxObjectiveScores[0])
                        {
                            MaxObjectiveScores[0] = b.Evaluation.ObjectiveFitness;
                        }
                    }
                }

                _previousGeneration = new Dictionary<TGenome, Behaviour<TGenome>>(combinedCount);
                for (int i = 0; i < combinedCount; i++)
                {
                    Behaviour<TGenome> b = combined[i];
                    _previousGeneration.Add(b.Genome, b);
                }
            }

            if (ReportInterval > 0 && _generation % ReportInterval == 0)
            {
                StringBuilder sb = new StringBuilder();

                if (MultiObjectiveEnabled)
                {
                    sb.Append("Max Objective scores:");
                    for (int i = 0; i < ObjectiveCount; i++)
                    {
                        sb.Append($" [{i}]: {MaxObjectiveScores[i]:F04}");
                    }

                    sb.Append($" Avg Pareto optimal behaviours: {_paretoOptimal / ReportInterval}");
                }

                sb.Append($" Avg time spent: [Evaluation: {_timeSpentEvaluation / ReportInterval} ms]");

                if (NoveltySearchEnabled)
                {
                    sb.Append($" [Novelty Search: {_timeSpentNoveltySearch / ReportInterval} ms]");
                }
                if (MultiObjectiveEnabled)
                {
                    foreach (IObjectiveScorer<TGenome> scorer in _objectiveScorers) {
                    
                        sb.Append($" [{scorer.Name}: {scorer.TimeSpentAccumulated / ReportInterval} ms]");
                        scorer.TimeSpentAccumulated = 0;
                    }
                    sb.Append($" [Multi Objective: {_timeSpentMultiObjective / ReportInterval} ms]");
                }

                _logger.Info(sb.ToString());

                _paretoOptimal = 0;

                _timeSpentEvaluation = 0;
                _timeSpentNoveltySearch = 0;
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
            Behaviour<TGenome> behaviour = new Behaviour<TGenome>(genome, ObjectiveCount);
            behaviour.Evaluation = _phenomeEvaluator.Evaluate(phenome);

            return behaviour;
        }

        /// <summary>
        /// Evaluate on multiple threads, according to the parallel options
        /// </summary>
        /// <param name="genomeList"></param>
        private IList<Behaviour<TGenome>> EvaluateParallel(IList<TGenome> genomeList)
        {
            if (_generation == 1)
            {
                int concurrencyLevel = _parallelOptions.MaxDegreeOfParallelism == -1
                ? Environment.ProcessorCount
                : Math.Min(Environment.ProcessorCount, _parallelOptions.MaxDegreeOfParallelism);
                _logger.Info($"Running parallel, number of threads: {concurrencyLevel}");
            }

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