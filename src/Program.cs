using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
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
        const string CONFIG_PATH = "Config/";
        const string CHAMPION_FILE = "champion.xml";
        const string RECORDING_FILE = "recording.png";

        private static NeatEvolutionAlgorithm<NeatGenome> _ea;
        private static IExperiment _experiment;

        public static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

        static void Main(string[] args)
        {
            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            Console.WriteLine("Select config");

            // Load the config files.
            // Config files must be copied into the output directory in the CONFIG_PATH folder
            List<string> configs = GetConfigs();
            for (int i = 0; i < configs.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {configs[i].Replace(CONFIG_PATH, string.Empty)}");
            }

            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                int selection = (int) char.GetNumericValue(key.KeyChar);

                if (selection > 0 && selection <= configs.Count)
                {
                    LoadExperiment(configs[selection - 1]);
                    break;
                }

                Console.WriteLine("Please select a valid config");
            } while (true);
            
            Console.WriteLine($"Controls:" +
                $"\n-{"Space:", -10} Start/Pause Evolutionary Algorithm" +
                $"\n-{"D:",-10} Toggle Debug (only available for debug builds)" +
                $"\n-{"C:",-10} Test current champion" +
                $"\n-{"S:",-10} Test saved champion (from champion xml)" +
                $"\n-{"Esc:",-10} Exit");

            // Start listening for input
            ProcessInput();
        }

        static List<string> GetConfigs()
        {
            return Directory.EnumerateFiles(CONFIG_PATH, "*.xml").ToList();
        }

        static void LoadExperiment(string configPath)
        {
            // Load config XML.
            XmlDocument xml = new XmlDocument();
            xml.Load(configPath);

            XmlElement config = xml.DocumentElement;

            Assembly assembly = Assembly.GetExecutingAssembly();

            Type experimentType = assembly.GetType(XmlUtils.GetValueAsString(config, "ExperimentClass"), false, false);
            _experiment = (IExperiment) Activator.CreateInstance(experimentType);

            _experiment.Initialize(XmlUtils.GetValueAsString(config, "Name"), config);
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
            Console.WriteLine("Testing phenome...");
            _experiment.Evaluate(phenome, 1, true);
            Bitmap bmp = _experiment.Recorder.ToBitmap();

            CreateExperimentDirectoryIfNecessary();
            bmp.Save(string.Format($"{_experiment.Name}/{RECORDING_FILE}"), ImageFormat.Png);

            Console.WriteLine("Done.");
        }

        private static void EAUpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine($"gen={_ea.CurrentGeneration}, bestFitness={_ea.Statistics._maxFitness.ToString("F4")}, meanFitness={_ea.Statistics._meanFitness.ToString("F4")}");

            // Save the best genome to file
            XmlDocument doc = NeatGenomeXmlIO.Save(_ea.CurrentChampGenome, false);

            CreateExperimentDirectoryIfNecessary();

            string file = string.Format($"{_experiment.Name}/{CHAMPION_FILE}");
            doc.Save(file);
        }

        private static void CreateExperimentDirectoryIfNecessary()
        {
            if (!Directory.Exists(_experiment.Name))
            {
                Directory.CreateDirectory(_experiment.Name);
            }
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
