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
using System.Threading.Tasks;
using System.Xml;
using ENTM.Replay;
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
    public abstract class NeatExperiment<TEvaluator, TEnviroment> : IExperiment
        where TEnviroment : IEnvironment, new()
        where TEvaluator : BaseEvaluator<TEnviroment>, new()
    {
        NeatEvolutionAlgorithmParameters _eaParams;
        NeatGenomeParameters _neatGenomeParams;
        string _name;
        int _populationSize;
        NetworkActivationScheme _activationScheme;
        string _complexityRegulationStr;
        int? _complexityThreshold;
        string _description;
        ParallelOptions _parallelOptions;
        bool _multiThreading;

        protected TEvaluator _evaluator;

        #region Abstract properties that subclasses must implement
        public abstract int InputCount { get; }
        public abstract int OutputCount { get; }
        #endregion

        /// <summary>
        /// Evaluate a single phenome a set number of iterations. Use this to test a champion
        /// </summary>
        /// <param name="phenome">The phenome to be tested</param>
        /// <param name="iterations">Number of evaluations</param>
        /// <param name="record">Determines if the evaluation should be recorded</param>
        /// <returns></returns>
        public double Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            return _evaluator.Evaluate(phenome, iterations, record);
        }

        public Recorder Recorder => _evaluator.Recorder;

        #region INeatExperiment Members

        public string Description => _description;

        public string Name => _name;

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
        /// Initialize the experiment with some optional XML configutation data.
        /// </summary>
        public void Initialize(string name, XmlElement xmlConfig)
        {
            _name = name;
            _populationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
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

            IGenomeListEvaluator<NeatGenome> innerEvaluator;
            if (_multiThreading)    innerEvaluator = new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, _evaluator, _parallelOptions);
            else                    innerEvaluator = new SerialGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, _evaluator);

            // Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
            // that were in the population in previous generations (elite genomes). This is determiend by examining each genome's evaluation info object.
            IGenomeListEvaluator<NeatGenome> selectiveEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(
                                                                                    innerEvaluator,
                                                                                    SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());
            // Initialize the evolution algorithm.
            ea.Initialize(selectiveEvaluator, genomeFactory, genomeList);


            // Finished. Return the evolution algorithm
            return ea;
        }

        #endregion

    }
}
