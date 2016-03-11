/* ***************************************************************************
 * This file is part of the NashCoding tutorial on SharpNEAT 2.
 * 
 * Copyright 2010, Wesley Tansey (wes@nashcoding.com)
 * 
 * Some code in this file may have been copied directly from SharpNEAT,
 * for learning purposes only. Any code copied from SharpNEAT 2 is 
 * copyright of Colin Green (sharpneat@gmail.com).
 *
 * Both SharpNEAT and this tutorial are free software: you can redistribute
 * it and/or modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the 
 * License, or (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ENTM.NoveltySearch;
using ENTM.Replay;
using ENTM.Utility;
using log4net;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using Debug = ENTM.Utility.Debug;

namespace ENTM.Experiments
{
    /// <summary>
    /// Helper class that hides most of the details of setting up an experiment.
    /// If you're just doing a simple console-based experiment, this is probably
    /// what you want to inherit from. However, if you need more flexibility
    /// (e.g., custom genome/phenome creation or performing complex population
    /// evaluations) then you probably want to implement your own INeatExperiment
    /// class.
    /// </summary>
    public abstract class BaseExperiment<TEvaluator, TEnviroment, TController> : ITuringExperiment
        where TEnviroment : IEnvironment
        where TEvaluator : BaseEvaluator<TEnviroment, TController>, new()
        where TController : IController
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ITuringExperiment));

        const string CHAMPION_FILE = "champion{0:D4}.xml";
        const string RECORDING_FILE = "recording{0:D4}.png";

        private const int LOG_INTERVAL = 100;
        private uint _lastLog;
        private long _lastLogTime;

        private NeatEvolutionAlgorithmParameters _eaParams;
        private NeatGenomeParameters _neatGenomeParams;
        private NoveltySearchParameters _noveltySearchParams;

        private NeatEvolutionAlgorithm<NeatGenome> _ea;
        protected TEvaluator _evaluator;
        protected NoveltySearchListEvaluator<NeatGenome, IBlackBox> _innerEvaluator;

        private string _name;
        private int _populationSize, _maxGenerations;
        private NetworkActivationScheme _activationScheme;
        private string _complexityRegulationStr;
        private int? _complexityThreshold;
        private string _description;
        private ParallelOptions _parallelOptions;
        private bool _multiThreading;


        private string ChampionFile => $"{CurrentDirectory}{string.Format(CHAMPION_FILE, _number)}";
        private string RecordingFile => $"{CurrentDirectory}{string.Format(RECORDING_FILE, _number)}";

        public bool ExperimentCompleted { get; private set; } = false;

        // Used for folder structure
        private string _identifier;
        private int _subIdentifier, _number;

        private bool _didStart = false;
        private bool _abort = false;

        public Recorder Recorder => _evaluator.Recorder;
        private Stopwatch _timer;

        public TimeSpan TimeSpent => _timer?.Elapsed ?? TimeSpan.Zero;

        public int InputCount => EnvironmentOutputCount + ControllerOutputCount;
        public int OutputCount => EnvironmentInputCount + ControllerInputCount;

        public void SetNoveltySearchEnabled(bool enabled)
        {
            _innerEvaluator.NoveltySearchEnabled = enabled;
            _evaluator.NoveltySearchEnabled = enabled;
        }

        #region Abstract properties that subclasses must implement
        public abstract int EnvironmentInputCount { get; }
        public abstract int EnvironmentOutputCount { get; }
        public abstract int ControllerInputCount { get; }
        public abstract int ControllerOutputCount { get; }
        #endregion

        /// <summary>
        /// Notifies listeners that the experiment has started
        /// </summary>
        public event EventHandler ExperimentStartedEvent;

        /// <summary>
        /// Notifies listeners that the experiment was paused
        /// </summary>
        public event EventHandler ExperimentPausedEvent;

        /// <summary>
        /// Notifies listeners that the experiment was resumed
        /// </summary>
        public event EventHandler ExperimentResumedEvent;

        /// <summary>
        /// Notifies listeners that the experiment is complete - that is; maximum generations or fitness was reached
        /// </summary>
        public event EventHandler ExperimentCompleteEvent;

        /// <summary>
        /// Test the champion of the current EA
        /// </summary>
        public FitnessInfo TestCurrentChampion()
        {
            if (_ea?.CurrentChampGenome == null)
            {
                Console.WriteLine("No current champion");
                return FitnessInfo.Zero;
            }

            IGenomeDecoder<NeatGenome, IBlackBox> decoder = CreateGenomeDecoder();

            IBlackBox champion = decoder.Decode(_ea.CurrentChampGenome);

            return TestPhenome(champion);
        }

        public FitnessInfo TestSavedChampion(string xmlPath)
        {
            // Load genome from the xml file
            XmlDocument xmlChampion = new XmlDocument();
            xmlChampion.Load(xmlPath);

            NeatGenome championGenome = NeatGenomeXmlIO.LoadGenome(xmlChampion.DocumentElement, false);

            // Create and set the genome factory
            championGenome.GenomeFactory = CreateGenomeFactory() as NeatGenomeFactory;

            // Create the genome decoder
            IGenomeDecoder<NeatGenome, IBlackBox> decoder = CreateGenomeDecoder();

            // Decode the genome (genotype => phenotype)
            IBlackBox champion = decoder.Decode(championGenome);

            return TestPhenome(champion);
        }

        private FitnessInfo TestPhenome(IBlackBox phenome)
        {
            if (_ea != null) _ea.RequestPause();
            else CreateEA();

            Debug.On = true;
            _logger.Info("Testing phenome...");

            FitnessInfo result = _evaluator.TestPhenome(phenome);

            Bitmap bmp = Recorder.ToBitmap();

            CreateExperimentDirectoryIfNecessary();
            bmp.Save(RecordingFile, ImageFormat.Png);

            _logger.Info($"Done. Achieved fitness: {result._fitness:F4}");

            return result;
        }

        public void AbortCurrentExperiment()
        {
            if (_didStart) _abort = true;
        }
 
        private void EAUpdateEvent(object sender, EventArgs e)
        {
            if (_lastLog == 0 || _ea.CurrentGeneration - _lastLog >= LOG_INTERVAL)
            {
                uint gensSinceLastLog = _ea.CurrentGeneration - _lastLog;
                if (gensSinceLastLog < 1) gensSinceLastLog = 1;

                _lastLog = _ea.CurrentGeneration;

                long spent = _timer.ElapsedMilliseconds - _lastLogTime;
                long timePerGen = spent / gensSinceLastLog;
                long gensRemaining = _maxGenerations - _ea.CurrentGeneration;
                long timeRemainingEst = gensRemaining * timePerGen;

                _lastLogTime = _timer.ElapsedMilliseconds;

                _logger.Info($"Generation: {_ea.CurrentGeneration}/{_maxGenerations}, "+
                  $"Time/gen: {timePerGen} ms, Est. time remaining: {Utilities.TimeToString(timeRemainingEst)} " +
                  $"Fitness - Max: {_ea.Statistics._maxFitness:F4} Mean: {_ea.Statistics._meanFitness:F4}, " +
                  $"Complexity - Max: {_ea.Statistics._maxComplexity:F4} Mean: {_ea.Statistics._meanComplexity:F4}, " +
                  $"Specie size - Max: {_ea.Statistics._maxSpecieSize:D} Min: {_ea.Statistics._minSpecieSize:D}"
                  );
            }


            // Save the best genome to file
            XmlDocument doc = NeatGenomeXmlIO.Save(_ea.CurrentChampGenome, false);

            CreateExperimentDirectoryIfNecessary();

            string file = string.Format(ChampionFile);
            doc.Save(file);

            // Check if the experiment has been aborted, the maximum generations count have been reached, or if the maximum fitness has been reached
            if (_abort || (_maxGenerations > 0 && _ea.CurrentGeneration >= _maxGenerations) || _evaluator.StopConditionSatisfied)
            {
                ExperimentCompleted = true;
                _ea.RequestPause();
            }
        }

        private void EAPauseEvent(object sender, EventArgs e)
        {
            _timer.Stop();

            _lastLog = 0;

            if (ExperimentCompleted)
            {
                if (_abort)
                {
                    _abort = false;
                    _logger.Info("Experiment aborted");
                }
                else
                {
                    _logger.Info("Experiment completed");
                }

                if (ExperimentCompleteEvent != null)
                {
                    try
                    {
                        ExperimentCompleteEvent(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                        _logger.Info("ExperimentCompleteEvent listener threw exception: " + ex.Message);
                    }
                }
            }
            else
            {
                _logger.Info("EA was paused");
                if (ExperimentPausedEvent != null)
                {
                    try
                    {
                        ExperimentPausedEvent(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                        _logger.Info("ExperimentPausedEvent listener threw exception: " + ex.Message);
                    }
                }
            }
        }

        public void StartStopEA()
        {
            if (_ea == null)
            {
                CreateEA();
                _ea.StartContinue();

            }
            else
            {
                switch (_ea.RunState)
                {
                    case SharpNeat.Core.RunState.NotReady:
                        _logger.Info("EA not ready!");
                        break;

                    case SharpNeat.Core.RunState.Ready:
                    case SharpNeat.Core.RunState.Paused:
                        _logger.Info("Starting EA...");
                        _timer.Start();

                        if (ExperimentResumedEvent != null)
                        {
                            try
                            {
                                ExperimentResumedEvent(this, EventArgs.Empty);
                            }
                            catch (Exception ex)
                            {
                                // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                                _logger.Info("ExperimentResumedEvent listener threw exception: " + ex.Message);
                            }
                        }
                        _ea.StartContinue();
                        break;

                    case SharpNeat.Core.RunState.Running:
                        _logger.Info("Pausing EA...");
                        _ea.RequestPause();
                        break;

                    case SharpNeat.Core.RunState.Terminated:
                        _logger.Info("EA was terminated");
                        break;
                }
            }
        }

        private void CreateEA()
        {
            _timer = new Stopwatch();
            _didStart = true;

            _timer.Start();

            _logger.Info("\nCreating EA...");
            // Create evolution algorithm and attach events.
            _ea = CreateEvolutionAlgorithm();
            _ea.UpdateEvent += EAUpdateEvent;
            _ea.PausedEvent += EAPauseEvent;

            if (ExperimentStartedEvent != null)
            {
                try
                {
                    ExperimentStartedEvent(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    // Catch exceptions thrown by even listeners. This prevents listener exceptions from terminating the algorithm thread.
                    _logger.Info("ExperimentStartedEvent listener threw exception: " + ex.Message);
                }
            }
        }

        private void CreateExperimentDirectoryIfNecessary()
        {
            if (!Directory.Exists(CurrentDirectory))
            {
                Directory.CreateDirectory(CurrentDirectory);
            }
        }

        #region INeatExperiment Members

        public string Description => _description;

        public string Name => _name;

        public string CurrentDirectory => string.Format($"{Program.ROOT_PATH}{Name}/{_identifier}/{_subIdentifier}/");

        /// <summary>
        /// Gets the default population size to use for the experiment
        /// </summary>
        public int DefaultPopulationSize => _populationSize;

        /// <summary>
        /// Gets the NeatGenomeParameters to be used for the experiment. Parameters on this object can be modified. Calls
        /// to CreateEvolutionAlgorithm() make a copy of and use this object in whatever state it is in at the time of the call.
        /// </summary>
        public NeatGenomeParameters NeatGenomeParameters => _neatGenomeParams;

        /// <summary>
        /// Gets the NeatEvolutionAlgorithmParameters to be used for the experiment. Parameters on this object can be 
        /// modified. Calls to CreateEvolutionAlgorithm() make a copy of and use this object in whatever state it is in 
        /// at the time of the call.
        /// </summary>
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters => _eaParams;

        /// <summary>
        /// Initialize the experiment with an identifier unique to a series of experiments, and the experiment number in that series.
        /// Used for persistence
        /// </summary>
        /// <param name="name"></param>
        /// <param name="xmlConfig"></param>
        /// <param name="identifier"></param>
        /// <param name="number"></param>
        public void Initialize(string name, XmlElement xmlConfig, string identifier, int subIdentifier, int number)
        {
            _identifier = identifier;
            _subIdentifier = subIdentifier;
            _number = number; 
            Initialize(name, xmlConfig);
        }

        /// <summary>
        /// Initialize the experiment with some optional XML configutation data.
        /// </summary>
        public void Initialize(string name, XmlElement xmlConfig)
        {
            _name = name;
            _populationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            _maxGenerations = XmlUtils.GetValueAsInt(xmlConfig, "MaxGenerations");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _complexityRegulationStr = XmlUtils.TryGetValueAsString(xmlConfig, "ComplexityRegulationStrategy");
            _complexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            _description = XmlUtils.TryGetValueAsString(xmlConfig, "Description");
            _parallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);
            _multiThreading = XmlUtils.TryGetValueAsBool(xmlConfig, "MultiThreading") ?? true;

            // Evolutionary algorithm parameters
            XmlElement xmlEAParams = xmlConfig.SelectSingleNode("EAParams") as XmlElement;
            _eaParams = new NeatEvolutionAlgorithmParameters();

            _eaParams.SpecieCount = XmlUtils.GetValueAsInt(xmlEAParams, "SpecieCount");
            _eaParams.ElitismProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "ElitismProportion");
            _eaParams.SelectionProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "SelectionProportion");
            _eaParams.OffspringAsexualProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "OffspringAsexualProportion");
            _eaParams.OffspringSexualProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "OffspringSexualProportion");
            _eaParams.InterspeciesMatingProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "InterspeciesMatingProportion");
            _eaParams.BestFitnessMovingAverageHistoryLength = XmlUtils.GetValueAsInt(xmlEAParams, "BestFitnessMovingAverageHistoryLength");
            _eaParams.MeanSpecieChampFitnessMovingAverageHistoryLength = XmlUtils.GetValueAsInt(xmlEAParams, "MeanSpecieChampFitnessMovingAverageHistoryLength");
            _eaParams.ComplexityMovingAverageHistoryLength = XmlUtils.GetValueAsInt(xmlEAParams, "ComplexityMovingAverageHistoryLength");

            // NEAT Genome parameters
            XmlElement xmlGenomeParams = xmlConfig.SelectSingleNode("GenomeParams") as XmlElement;
            _neatGenomeParams = new NeatGenomeParameters();

            // Prevent recurrent connections if the activation scheme is acyclic
            _neatGenomeParams.FeedforwardOnly = _activationScheme.AcyclicNetwork;
            _neatGenomeParams.ConnectionWeightRange = XmlUtils.GetValueAsDouble(xmlGenomeParams, "ConnectionWeightRange");
            _neatGenomeParams.InitialInterconnectionsProportion = XmlUtils.GetValueAsDouble(xmlGenomeParams, "InitialInterconnectionsProportion");
            _neatGenomeParams.DisjointExcessGenesRecombinedProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "DisjointExcessGenesRecombinedProbability");
            _neatGenomeParams.ConnectionWeightMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "ConnectionWeightMutationProbability");
            _neatGenomeParams.AddNodeMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "AddNodeMutationProbability");
            _neatGenomeParams.AddConnectionMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "AddConnectionMutationProbability");
            _neatGenomeParams.NodeAuxStateMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "NodeAuxStateMutationProbability");
            _neatGenomeParams.DeleteConnectionMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "DeleteConnectionMutationProbability");


            XmlElement xmlNoveltySearchParams = xmlConfig.SelectSingleNode("NoveltySearch") as XmlElement;
            _noveltySearchParams = NoveltySearchParameters.ReadXmlProperties(xmlNoveltySearchParams);

            // Create IBlackBox evaluator.
            _evaluator = new TEvaluator();
            _evaluator.Initialize(xmlConfig);
        }

        /// <summary>
        /// Load a population of genomes from an XmlReader and returns the genomes in a new list.
        /// The genome factory for the genomes can be obtained from any one of the genomes.
        /// </summary>
        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory) CreateGenomeFactory();
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
        }

        /// <summary>
        /// Save a population of genomes to an XmlWriter.
        /// </summary>
        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            // Writing node IDs is not necessary for NEAT.
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, false);
        }

        /// <summary>
        /// Create a genome decoder for the experiment.
        /// </summary>
        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new NeatGenomeDecoder(_activationScheme);
        }

        /// <summary>
        /// Create a genome factory for the experiment.
        /// Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
        /// </summary>
        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new NeatGenomeFactory(InputCount, OutputCount, _neatGenomeParams);
        }

        /// <summary>
        /// Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts
        /// of the algorithm are also constructed and connected up.
        /// Uses the experiments default population size defined in the experiment's config XML.
        /// </summary>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            return CreateEvolutionAlgorithm(_populationSize);
        }

        /// <summary>
        /// Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts
        /// of the algorithm are also constructed and connected up.
        /// This overload accepts a population size parameter that specifies how many genomes to create in an initial randomly
        /// generated population.
        /// </summary>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }


        /// <summary>
        /// Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts
        /// of the algorithm are also constructed and connected up.
        /// This overload accepts a pre-built genome population and their associated/parent genome factory.
        /// </summary>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, _parallelOptions);

            // Create complexity regulation strategy.
            IComplexityRegulationStrategy complexityRegulationStrategy = ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStr, _complexityThreshold);

            // Create the evolution algorithm.
            NeatEvolutionAlgorithm<NeatGenome> ea = new NeatEvolutionAlgorithm<NeatGenome>(_eaParams, speciationStrategy, complexityRegulationStrategy);

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

            INoveltyScorer<NeatGenome> noveltyScorer = new TuringNoveltyScorer<NeatGenome>(_noveltySearchParams);
            
            _innerEvaluator = new NoveltySearchListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, _evaluator, noveltyScorer, _multiThreading, _parallelOptions);

            SetNoveltySearchEnabled(_noveltySearchParams.Enabled);

            // Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
            // that were in the population in previous generations (elite genomes). This is determiend by examining each genome's evaluation info object.
            IGenomeListEvaluator<NeatGenome> selectiveEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(
                                                                                    _innerEvaluator,
                                                                                    SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());
            // Initialize the evolution algorithm.
            ea.Initialize(selectiveEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return ea;
        }
        #endregion
    }
}
