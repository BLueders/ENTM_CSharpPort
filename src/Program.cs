using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Experiments.CopyTask;
using ENTM.Utility;
using log4net.Config;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Domains;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using ENTM.Experiments;
using System.Threading;

namespace ENTM
{
    class Program
    {
        const string CHAMPION_FILE = "copytask_champion.xml";

        private static NeatEvolutionAlgorithm<NeatGenome> _ea;
        private static CopyTaskExperiment _experiment;

        public static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

        static void Main(string[] args)
        {
            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Experiment classes encapsulate much of the nuts and bolts of setting up a NEAT search.
            _experiment = new CopyTaskExperiment();

            // Load config XML.
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load("copytask.config.xml");
            _experiment.Initialize("Copy Task", xmlConfig.DocumentElement);

            Console.WriteLine($"Controls:" +
                $"\n-{"Space:", -10} Start/Pause Evolutionary Algorithm" +
                $"\n-{"D:",-10} Toggle Debug (only available for debug builds)" +
                $"\n-{"C:",-10} Test current champion" +
                $"\n-{"S:",-10} Test saved champion (from champion xml)" +
                $"\n-{"Esc:",-10} Exit");

            // Start listening for input
            ProcessInput();
        }

        /// <summary>
        /// Test the champion of the current EA
        /// </summary>
        private static void TestCurrentChampion()
        {
            if (_ea?.CurrentChampGenome == null)
            {
                Console.WriteLine("No current champion");
                return;
            }

            _ea.RequestPause();

            IGenomeDecoder<NeatGenome, IBlackBox> decoder = _experiment.CreateGenomeDecoder();

            IBlackBox champion = decoder.Decode(_ea.CurrentChampGenome);
            TestPhenome(champion);
        }

        private static void TestSavedChampion()
        {
            // Load genome from the xml file
            XmlDocument xmlChamp = new XmlDocument();
            xmlChamp.Load(CHAMPION_FILE);
            NeatGenome champGenome = NeatGenomeXmlIO.LoadGenome(xmlChamp.DocumentElement, false);

            // Create and set the genome factory
            champGenome.GenomeFactory = _experiment.CreateGenomeFactory() as NeatGenomeFactory;

            // Create the genome decoder
            IGenomeDecoder<NeatGenome, IBlackBox> decoder = _experiment.CreateGenomeDecoder();
            
            // Decode the genome (genotype => phenotype)
            IBlackBox champion = decoder.Decode(champGenome);

            TestPhenome(champion);
        }

        private static void TestPhenome(IBlackBox phenome)
        {
            Debug.On = true;
            Console.WriteLine("\n");
            Debug.LogHeader("TESTING PHENOME", false);
            _experiment.Evaluator.Evaluate(phenome, 1);
        }

        private static void EAUpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine("gen={0:N0} bestFitness={1:N6}", _ea.CurrentGeneration, _ea.Statistics._maxFitness);

            // Save the best genome to file
            XmlDocument doc = NeatGenomeXmlIO.Save(_ea.CurrentChampGenome, false);
            doc.Save(CHAMPION_FILE);
        }

        private static void EAPauseEvent(object sender, EventArgs e)
        {
            Console.WriteLine("EA was paused");
        }

        private static void StartStopEA()
        {
            if (_ea == null)
            {
                Console.WriteLine("Creating EA...");
                // Create evolution algorithm and attach events.
                _ea = _experiment.CreateEvolutionAlgorithm();
                _ea.UpdateEvent += EAUpdateEvent;
                _ea.PausedEvent += EAPauseEvent;

                _ea.StartContinue();
            }
            else
            {
                switch (_ea.RunState)
                {
                    case SharpNeat.Core.RunState.NotReady:
                        Console.WriteLine("EA not ready!");
                        break;

                    case SharpNeat.Core.RunState.Ready:
                    case SharpNeat.Core.RunState.Paused:
                        Console.WriteLine("Starting EA...");
                        _ea.StartContinue();
                        break;

                    case SharpNeat.Core.RunState.Running:
                        Console.WriteLine("Pausing EA...");
                        _ea.RequestPause();
                        break;

                    case SharpNeat.Core.RunState.Terminated:
                        Console.WriteLine("EA was terminated");
                        break;
                }
            }
        }

        private static void ProcessInput()
        {
            do
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.Spacebar:
                        StartStopEA();
                        break;

                    case ConsoleKey.D:
#if DEBUG
                        Debug.On = !Debug.On;
                        if (Debug.On) Console.WriteLine("Debug on.");
                        else Console.WriteLine("Debug off. Press D to turn Debug back on");
#else
                        Console.WriteLine("Debug not available");
#endif
                        break;

                    case ConsoleKey.C:
                        TestCurrentChampion();
                        break;

                    case ConsoleKey.S:
                        TestSavedChampion();
                        break;

                    case ConsoleKey.Escape:
                        return;
                }

            } while (true);
        }
    }
}
