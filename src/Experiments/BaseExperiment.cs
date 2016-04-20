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
using System.Globalization;
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
        private static readonly ILog _logger = LogManager.GetLogger("Experiment");

        const string CHAMPION_FILE = "champion{0:D4}.xml";
        const string RECORDING_FILE = "recording{0:D4}_{1}.png";
        const string DATA_FILE = "data{0:D4}.csv";

        private NeatEvolutionAlgorithmParameters _eaParams;
        private NeatGenomeParameters _neatGenomeParams;
        private NoveltySearchParameters _noveltySearchParams;

        private NeatEvolutionAlgorithm<NeatGenome> _ea;
        protected TEvaluator _evaluator;
        protected NoveltySearchListEvaluator<NeatGenome, IBlackBox> _listEvaluator;

        private string _name;
        private int _populationSize, _maxGenerations;
        private NetworkActivationScheme _activationScheme;
        private IComplexityRegulationStrategy _complexityRegulationStrategy;
        private string _complexityRegulationStr;
        private int? _complexityThreshold;
        private string _description;
        private ParallelOptions _parallelOptions;
        private bool _multiThreading;
        private int _logInterval;

        private uint _lastLog;
        private long _lastLogTime;
        private double _currentMaxFitness = -1;
        private uint _lastMaxFitnessImprovementGen;
        private uint _noveltySearchActivatedGen;

        private string ChampionFile => $"{CurrentDirectory}{string.Format(CHAMPION_FILE, _number)}";
        private string DataFile => $"{CurrentDirectory}{string.Format(DATA_FILE, _number)}";

        private string RecordingFile(uint id)
        {
            return $"{CurrentDirectory}{string.Format(RECORDING_FILE, _number, id)}";
        }

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

        private bool _noveltySearchEnabled;
        public bool NoveltySearchEnabled
        {
            get { return _noveltySearchEnabled; }
            set
            {
                _noveltySearchEnabled = value;
                _listEvaluator.NoveltySearchEnabled = _noveltySearchEnabled;
                _evaluator.NoveltySearchEnabled = _noveltySearchEnabled;

                _currentMaxFitness = 0;

                if (_noveltySearchEnabled)
                {
                    if (_ea != null)
                    {
                        _noveltySearchActivatedGen = _ea.CurrentGeneration;
                    }
                    else
                    {
                        _noveltySearchActivatedGen = 0;
                    }
                }

                _logger.Info($"Novelty search {(_noveltySearchEnabled ? "enabled" : "disabled")}");
            }
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

            return TestGenome(_ea.CurrentChampGenome, 1);
        }

        public void TestCurrentPopulation()
        {
            if (_ea == null)
            {
                Console.WriteLine("No current population");
                return;
            }

            Console.WriteLine("Testing current population...");

            foreach (NeatGenome genome in _ea.GenomeList)
            {
                TestGenome(genome, 1);
            }
        }

        public FitnessInfo TestSavedChampion(string xmlPath)
        {
            return TestSavedChampion(xmlPath, 1);
        }

        public FitnessInfo TestSavedChampion(string xmlPath, int iterations)
        {
            // Load genome from the xml file
            XmlDocument xmlChampion = new XmlDocument();
            xmlChampion.Load(xmlPath);

            NeatGenome championGenome = NeatGenomeXmlIO.LoadGenome(xmlChampion.DocumentElement, false);

            // Create and set the genome factory
            championGenome.GenomeFactory = CreateGenomeFactory() as NeatGenomeFactory;


            return TestGenome(championGenome, iterations);
        }

        private FitnessInfo TestGenome(NeatGenome genome, int iterations)
        {
            if (_ea != null && _ea.RunState == RunState.Running)
            {
                _ea.RequestPause();
            }

            // Create the genome decoder
            IGenomeDecoder<NeatGenome, IBlackBox> decoder = CreateGenomeDecoder();

            // Decode the genome (genotype => phenotype)
            IBlackBox phenome = decoder.Decode(genome);

            Debug.On = true;
            _logger.Info($"Testing phenome (ID: {genome.Id})...");

            FitnessInfo result = _evaluator.TestPhenome(phenome, iterations);

            CreateExperimentDirectoryIfNecessary();

            if (Recorder != null)
            {
                Bitmap bmp = Recorder.ToBitmap();
                bmp.Save($"{RecordingFile(genome.Id)}", ImageFormat.Png);
            }
            else
            {
                _logger.Warn("Recorder was null");
            }


            _logger.Info($"Done. Achieved fitness: {result._fitness:F4}");

            return result;
        }

        public void AbortCurrentExperiment()
        {
            if (_didStart) _abort = true;
        }

        private void PrintEAStats()
        {
            uint gensSinceLastLog = _ea.CurrentGeneration - _lastLog;

            if (gensSinceLastLog < 1) gensSinceLastLog = 1;

            float[] assd = new float[] {2f, 4f};

            _lastLog = _ea.CurrentGeneration;

            long spent = _timer.ElapsedMilliseconds - _lastLogTime;
            long timePerGen = spent / gensSinceLastLog;
            long gensRemaining = _maxGenerations - _ea.CurrentGeneration;
            long timeRemainingEst = gensRemaining * timePerGen;

            uint gen = _ea.CurrentGeneration;
            double fitMax = _ea.Statistics._maxFitness;
            double fitMean = _ea.Statistics._meanFitness;
            double cmpMax = _ea.Statistics._maxComplexity;
            double cmpMean = _ea.Statistics._meanComplexity;
            double cmpChamp = _ea.CurrentChampGenome.Complexity;
            int spcMax = _ea.Statistics._maxSpecieSize;
            int spcMin = _ea.Statistics._minSpecieSize;

            _lastLogTime = _timer.ElapsedMilliseconds;
            _logger.Info($"Generation: {gen}/{_maxGenerations}, " +
              $"Time/gen: {timePerGen} ms, Est. time remaining: {Utilities.TimeToString(timeRemainingEst)} " +
              $"Fitness - Max: {fitMax:F4} Mean: {fitMean:F4}, " +
              $"Complexity - Max: {cmpMax:F0} Mean: {cmpMean:F2} " +
              $"Champ: {cmpChamp:F0} Strategy: {_ea.ComplexityRegulationMode}, " +
              $"Specie size - Max: {spcMax:D} Min: {spcMin:D}, " +
              $"Generations since last improvement: {_ea.CurrentGeneration - _lastMaxFitnessImprovementGen}"
              );
        }

        private void WriteData()
        {
            uint gen = _ea.CurrentGeneration;
            double fitMax = _ea.Statistics._maxFitness;
            double fitMean = _ea.Statistics._meanFitness;
            double cmpMax = _ea.Statistics._maxComplexity;
            double cmpMean = _ea.Statistics._meanComplexity;
            double cmpChamp = _ea.CurrentChampGenome.Complexity;
            int spcMax = _ea.Statistics._maxSpecieSize;
            int spcMin = _ea.Statistics._minSpecieSize;

            if (!File.Exists(DataFile))
            {
                CreateExperimentDirectoryIfNecessary();
                using (StreamWriter sw = File.AppendText(DataFile))
                {
                    sw.WriteLine($"Generation,Max Fitness,Mean Fitness,Max Complexity,Mean Complexity,Champion Complexity,Max Specie Size,Min Specie Size");
                }
            }

            using (StreamWriter sw = File.AppendText(DataFile))
            {
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1:F4},{2:F4},{3:F0},{4:F4},{5:F0},{6},{7}",
                    gen, fitMax, fitMean, cmpMax, cmpMean, cmpChamp, spcMax, spcMin));
            }
        }

        private void EAUpdateEvent(object sender, EventArgs e)
        {
            if (_ea.Statistics._maxFitness > _currentMaxFitness)
            {
                _currentMaxFitness = _ea.Statistics._maxFitness;
                _lastMaxFitnessImprovementGen = _ea.CurrentGeneration;
            }

            if (_lastLog == 0 || _ea.CurrentGeneration - _lastLog >= _logInterval)
            {
                PrintEAStats();
            }

            if (_ea.CurrentGeneration > 0) WriteData();

            // Novelty search 
            if (_noveltySearchParams.Enabled)
            {
                if (NoveltySearchEnabled)
                {
                    // Novelty search has been completed, so we switch to objective search using the archive as seeded generation.
                    if (_listEvaluator.NoveltySearchComplete)
                    {
                        CreateEAFromNoveltyArchive();
                    }
                }
                else
                {
                    if (_ea.CurrentGeneration - _lastMaxFitnessImprovementGen > 1000)
                    {
                        _logger.Info("1000 gens passed since last improvement " + _ea.CurrentGeneration + " " + _lastMaxFitnessImprovementGen);
                        NoveltySearchEnabled = true;
                    }
                }
            }


            // Check if the experiment has been aborted, the maximum generations count have been reached, or if the maximum fitness has been reached
            if (_abort || (_maxGenerations > 0 && _ea.CurrentGeneration >= _maxGenerations) || _evaluator.StopConditionSatisfied)
            {
                PrintEAStats();
                ExperimentCompleted = true;
                _ea.RequestPause();
            }

            // Save the best genome to file
            XmlDocument doc = NeatGenomeXmlIO.Save(_ea.CurrentChampGenome, false);

            CreateExperimentDirectoryIfNecessary();

            string file = string.Format(ChampionFile);
            doc.Save(file);
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
                    case RunState.NotReady:
                        _logger.Info("EA not ready!");
                        break;

                    case RunState.Ready:
                    case RunState.Paused:
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

                    case RunState.Running:
                        _logger.Info("Pausing EA...");
                        _ea.RequestPause();
                        break;

                    case RunState.Terminated:
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

            NoveltySearchEnabled = _noveltySearchParams.Enabled;

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

        private void CreateEAFromNoveltyArchive()
        {
            _ea.UpdateEvent -= EAUpdateEvent;
            _ea.PausedEvent -= EAPauseEvent;

            _ea.Stop();

            List<NeatGenome> seedList = _listEvaluator.Archive;
            _logger.Info($"Creating seeded EA from novelty archive (size {seedList.Count})...");

            _ea = CreateEvolutionAlgorithm(_populationSize, _ea.CurrentGeneration, seedList);

            _ea.UpdateEvent += EAUpdateEvent;
            _ea.PausedEvent += EAPauseEvent;

            NoveltySearchEnabled = false;
            _lastMaxFitnessImprovementGen = 0;

            _ea.StartContinue();
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
            _complexityRegulationStrategy = ExperimentUtils.CreateComplexityRegulationStrategy(xmlConfig, "ComplexityRegulation");
            _description = XmlUtils.TryGetValueAsString(xmlConfig, "Description");
            _parallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);
            _multiThreading = XmlUtils.TryGetValueAsBool(xmlConfig, "MultiThreading") ?? true;
            _logInterval = XmlUtils.TryGetValueAsInt(xmlConfig, "LogInterval") ?? 10;

            // Evolutionary algorithm parameters
            _eaParams = new NeatEvolutionAlgorithmParameters();

            XmlElement xmlEAParams = xmlConfig.SelectSingleNode("EAParams") as XmlElement;

            if (xmlEAParams != null)
            {
                _eaParams.SpecieCount = XmlUtils.GetValueAsInt(xmlEAParams, "SpecieCount");
                _eaParams.ElitismProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "ElitismProportion");
                _eaParams.SelectionProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "SelectionProportion");
                _eaParams.OffspringAsexualProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "OffspringAsexualProportion");
                _eaParams.OffspringSexualProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "OffspringSexualProportion");
                _eaParams.InterspeciesMatingProportion = XmlUtils.GetValueAsDouble(xmlEAParams, "InterspeciesMatingProportion");
                _eaParams.BestFitnessMovingAverageHistoryLength = XmlUtils.GetValueAsInt(xmlEAParams, "BestFitnessMovingAverageHistoryLength");
                _eaParams.MeanSpecieChampFitnessMovingAverageHistoryLength = XmlUtils.GetValueAsInt(xmlEAParams, "MeanSpecieChampFitnessMovingAverageHistoryLength");
                _eaParams.ComplexityMovingAverageHistoryLength = XmlUtils.GetValueAsInt(xmlEAParams, "ComplexityMovingAverageHistoryLength");
            }
            else
            {
                _logger.Info("EA parameters not found. Using default.");
            }

            // NEAT Genome parameters
            _neatGenomeParams = new NeatGenomeParameters();
            
            // Prevent recurrent connections if the activation scheme is acyclic
            _neatGenomeParams.FeedforwardOnly = _activationScheme.AcyclicNetwork;

            XmlElement xmlGenomeParams = xmlConfig.SelectSingleNode("GenomeParams") as XmlElement;

            if (xmlGenomeParams != null)
            {
                _neatGenomeParams.ConnectionWeightRange = XmlUtils.GetValueAsDouble(xmlGenomeParams, "ConnectionWeightRange");
                _neatGenomeParams.InitialInterconnectionsProportion = XmlUtils.GetValueAsDouble(xmlGenomeParams, "InitialInterconnectionsProportion");
                _neatGenomeParams.DisjointExcessGenesRecombinedProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "DisjointExcessGenesRecombinedProbability");
                _neatGenomeParams.ConnectionWeightMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "ConnectionWeightMutationProbability");
                _neatGenomeParams.AddNodeMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "AddNodeMutationProbability");
                _neatGenomeParams.AddConnectionMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "AddConnectionMutationProbability");
                _neatGenomeParams.NodeAuxStateMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "NodeAuxStateMutationProbability");
                _neatGenomeParams.DeleteConnectionMutationProbability = XmlUtils.GetValueAsDouble(xmlGenomeParams, "DeleteConnectionMutationProbability");
            }
            else
            {
                _logger.Info("Genome parameters not found. Using default.");
            }

            XmlElement xmlNoveltySearchParams = xmlConfig.SelectSingleNode("NoveltySearch") as XmlElement;

            if (xmlNoveltySearchParams != null)
            {
                _noveltySearchParams = NoveltySearchParameters.ReadXmlProperties(xmlNoveltySearchParams);
            }
            else
            {
                _logger.Info("Novelty search parameters not found");
            }

            // Create IBlackBox evaluator.
            _evaluator = new TEvaluator();
            _evaluator.Initialize(xmlConfig);
            _evaluator.NoveltySearchParameters = _noveltySearchParams;
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
        /// Create and return a NeatEvolutionAlgorithm with a seeded population.
        /// </summary>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize, uint birthGeneration, List<NeatGenome> seedList)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, birthGeneration, seedList);

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

            // Create the evolution algorithm.
            NeatEvolutionAlgorithm<NeatGenome> ea = new NeatEvolutionAlgorithm<NeatGenome>(_eaParams, speciationStrategy, _complexityRegulationStrategy);

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

            INoveltyScorer<NeatGenome> noveltyScorer = new TuringNoveltyScorer<NeatGenome>(_noveltySearchParams);
            
            _listEvaluator = new NoveltySearchListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, _evaluator, noveltyScorer, _multiThreading, _parallelOptions);

            // Initialize the evolution algorithm.
            ea.Initialize(_listEvaluator, genomeFactory, genomeList);

            // Finished. Return the evolution algorithm
            return ea;
        }
        #endregion
    }
}
