using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using ENTM.Utility;
using log4net.Config;
using SharpNeat.Domains;
using ENTM.Experiments;
using System.Threading;
using ENTM.Base;
using ENTM.TuringMachine;
using log4net;

namespace ENTM
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        public const string ROOT_PATH = "ENTM/";
        private const string CONFIG_PATH = ROOT_PATH + "Config/";
        private const string LOG4NET_CONFIG = "log4net.properties";
        private const string RESULTS_FILE = "results.csv";
        private const string CONFIG_FILE = "config.xml";
        private const string DATA_FILE_GEN = "generalization.csv";

        private static readonly Stopwatch _stopwatch = new Stopwatch();

        private static string _currentDir;
        private static readonly Stack<string> _dirStack = new Stack<string>();

        private static bool _terminated = false;
        private static Type _experimentType;
        private static XmlDocument[] _configs;
        private static IExperiment _experiment;
        private static readonly string _identifier = string.Format($"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}_{Guid.NewGuid().ToString().Substring(0, 8)}");

        private static bool _didStartExperiment = false;
        private static bool _inputEnabled;
        private static int _currentConfig = -1;
        private static int _currentExperiment;
        private static int _experiementCount;
        private static DateTime _experimentStartedTime;

        private static bool _generalizeAllChamps = false;

        public static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

        static void Main(string[] args)
        {
            string[] configs;


            if (args.Length > 0)
            {
                _inputEnabled = false;
                configs = ParseArgs(args);
            }
            else
            {
                _inputEnabled = true;
                Console.WriteLine("No args found, prompting...");
                configs = Prompt();
            }

            LoadExperiments(configs);

            if (_generalizeAllChamps)
            {
                GeneralizeAllChamps();
            }
            else
            {

                if (_inputEnabled) PrintOptions();

                NextExperiment();

                // Start listening for input from console
                if (_inputEnabled) ProcessInput();
                else
                {
                    while (!_terminated);
                }
            }

        }

        private static string[] ParseArgs(string[] args)
        {
            string[] configs = null;

            if (args[0].EndsWith(".txt"))
            {
                configs = File.ReadAllLines($"{CONFIG_PATH}{args[0]}");
            }
            else
            {
                configs = args;
            }

            for (int i = 0; i < configs.Length; i++)
            {
                string config = configs[i];
                if (!config.EndsWith(".config.xml"))
                {
                    config = config.EndsWith(".config") ? $"{config}.xml" : $"{config}.config.xml";
                }
                configs[i] = $"ENTM/Config/{config}";
            }

            return configs;
        }

        private static string[] Prompt()
        {
            Console.WriteLine("Select config:");

            Console.WriteLine($"A: Execute all experiments in the current directory serially");
            Console.WriteLine($"G: Generalization test of all champion genomes in a folder. The folder must contain a matching config.xml");

            _currentDir = CONFIG_PATH;
            _dirStack.Push(ROOT_PATH);

            string[] configs = Browse();

            return configs;
        }

        private static void PrintOptions()
        {
            Console.WriteLine(InputOption.PrintAll());
        }

        private static string[] Browse()
        {
            Console.WriteLine($"\nCurrent directory: {_currentDir}");

            if (_dirStack.Count > 0)
            {
                Console.WriteLine($"0: [..]");
            }

            int select = 1;
            string[] folders = Directory.GetDirectories(_currentDir);
            for (int i = 0; i < folders.Length; i++)
            {
                Console.WriteLine($"{select++}: [{folders[i].Replace(_currentDir, string.Empty)}]");
            }

            // Load the config files.
            // Config files must be copied into the output directory in the CONFIG_PATH folder
            string[] xmls = Directory.EnumerateFiles(_currentDir, "*.xml").ToArray();
            for (int i = 0; i < xmls.Length; i++)
            {
                Console.WriteLine($"{select++}: {xmls[i].Replace(_currentDir, string.Empty)}");
            }

            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.A)
                {
                    return xmls.ToArray();
                }

                if (key.Key == ConsoleKey.G)
                {
                    _generalizeAllChamps = true;
                    return Directory.GetFiles(_currentDir, "config.xml");
                }

                int selection = (int) char.GetNumericValue(key.KeyChar);
                if (selection == 0)
                {
                    _currentDir = _dirStack.Pop();
                    return Browse();
                }

                if (selection > 0)
                {
                    if (selection <= folders.Length)
                    {
                        _dirStack.Push(_currentDir);
                        _currentDir = folders[selection - 1];
                        return Browse();
                    }

                    if (selection <= folders.Length + xmls.Length)
                    {
                        selection -= folders.Length;
                        return new[] {xmls[selection - 1]};
                    }
                } 

                Console.WriteLine("Please select a valid config");
            } while (true);
        }

        private static void LoadExperiments(string[] configPaths)
        {
            _configs = new XmlDocument[configPaths.Length];

            for (int i = 0; i < configPaths.Length; i++)
            {
                // Load config XML.
                XmlDocument xml = new XmlDocument();

                try
                {
                    xml.Load(configPaths[i]);
                    _configs[i] = xml;

                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"File not found: {configPaths[i]}");
                }
            }

            Console.WriteLine($"Loaded {_configs.Length} configs");
        }

        private static bool InitializeExperiment(XmlDocument config)
        {
            // Termine any ongoing experiment, or the algorithm thread will block indefinitely and leak
            _experiment?.Terminate();

            if (config == null)
            {
                Console.WriteLine("Config was null, aborting...");
                return false;
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            XmlElement xmlConfig = config.DocumentElement;

            _experimentType = assembly.GetType(XmlUtils.GetValueAsString(xmlConfig, "ExperimentClass"), false, false);
            _experiementCount = XmlUtils.TryGetValueAsInt(xmlConfig, "ExperimentCount") ?? 1;

            _experiment = (IExperiment) Activator.CreateInstance(_experimentType);

            string name = XmlUtils.GetValueAsString(xmlConfig, "Name");

            _logger.Info($"Initializing experiment: {name}...");

            // Register event listeners
            _experiment.ExperimentStartedEvent += ExperimentStartedEvent;
            _experiment.ExperimentPausedEvent += ExperimentPausedEvent;
            _experiment.ExperimentResumedEvent += ExperimentResumedEvent;
            _experiment.ExperimentCompleteEvent += ExperimentCompleteEvent;

            _experimentStartedTime = DateTime.Now;
            _experiment.Initialize(name, xmlConfig, _identifier, _currentConfig, _currentExperiment);
            
            return true;
        }
        

        private static void ExperimentStartedEvent(object sender, ExperimentEventArgs e)
        {
            _stopwatch.Start();
            _logger.Info($"Started experiment {_experiment.Name} {_currentExperiment}/{_experiementCount}");

            string configFile = $"{e.Directory}{CONFIG_FILE}";
            if (!File.Exists(configFile))
            {
                try
                {
                    _configs[_currentConfig].Save(configFile);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Could not write to config file: {ex.Message}", ex);
                }
            }
        }

        private static void ExperimentPausedEvent(object sender, EventArgs e)
        {
            _logger.Info($"Paused experiment {_experiment.Name} {_currentExperiment}");
            _stopwatch.Stop();
        }

        private static void ExperimentResumedEvent(object sender, EventArgs e)
        {
            _logger.Info($"Resumed experiment {_experiment.Name} {_currentExperiment}");
            _stopwatch.Start();
        }

        private static void ExperimentCompleteEvent(object sender, ExperimentCompleteEventArgs e)
        {
            string timeSpent = Utilities.TimeToString(e.TimeSpent);
            _logger.Info($"Time spent: {timeSpent}");

            EvaluationInfo testedFitness = _experiment.TestCurrentChampion(100);
            EvaluationInfo testedGenFitness = _experiment.TestCurrentChampionGeneralization(100);

            string resultsFile = $"{e.Directory}{RESULTS_FILE}";
            bool writeHeader = !File.Exists(resultsFile);

            try
            {
                using (StreamWriter sw = File.AppendText(resultsFile))
                {
                    if (writeHeader)
                    {
                        sw.WriteLine(
                            "Experiment,Comment,Start Time,Time,Solved,Generations,Champion Fitness,Champion Complexity,Champion Hidden Nodes,Champion Birth Generation,Tested Fitness,Tested Generalization Fitness");
                    }

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},\"{1}\",{9},{2},{3},{4},{5:F4},{6:F0},{7},{8},{10:F4},{11:F4}",
                        e.Experiment, e.Comment, timeSpent, e.Solved, e.Generations, e.ChampFitness, e.ChampComplexity,
                        e.ChampHiddenNodes, e.ChampBirthGen, _experimentStartedTime.ToString("ddMMyyyy-HHmmss"),
                        testedFitness.ObjectiveFitnessMean, testedGenFitness.ObjectiveFitnessMean));
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not write to results file: {ex.Message}", ex);
            }
         

            NextExperiment();
        }

        private static void NextExperiment()
        {
            if (_currentConfig < 0 || _currentExperiment >= _experiementCount)
            {
                NextConfig();
            }
            else
            {
                _currentExperiment++;
            }

            if (!_terminated)
            {

                if (InitializeExperiment(_configs[_currentConfig]))
                {
                    if (!_inputEnabled || _didStartExperiment)
                    {
                        _experiment.StartStopEA();
                    }
                    else
                    {
                        Console.WriteLine("Press [Spacebar] to start the experiment...");
                    }
                }
                else
                {
                    NextConfig();
                }
            }
        }

        private static void NextConfig()
        {
            if (_currentConfig < _configs.Length - 1)
            {
                _currentConfig++;
                _currentExperiment = 1;

                string name = XmlUtils.GetValueAsString(_configs[_currentConfig].DocumentElement, "Name");

                // Initialize logging
                GlobalContext.Properties["name"] = name;
                GlobalContext.Properties["id"] = _identifier;
                GlobalContext.Properties["count"] = $"{_currentConfig + 1:D4}";
                XmlConfigurator.Configure(new FileInfo(LOG4NET_CONFIG));

                // Print config
                ConfigPrinter.Print(_configs[_currentConfig].DocumentElement);
            }
            else
            {
                _terminated = true;
                _logger.Info($"All experiments completed. Total time spent: {Utilities.TimeToString(_stopwatch.Elapsed)}");
                if (_inputEnabled) Console.WriteLine("\nPress any key to exit...");
            }
        }

        private static void GeneralizeAllChamps()
        {
            string[] xmls = Directory.EnumerateFiles(_currentDir, "champion*.xml").ToArray();
            Console.WriteLine($"Found {xmls.Length} champion genomes");

            int count = xmls.Length;
            int iterations = GetIntegerConsoleInput("Enter number of iterations:");

            NextExperiment();

            EvaluationInfo[] results = new EvaluationInfo[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = _experiment.TestSavedChampion(xmls[i], iterations, 1, true, true)[0];
            }

            try
            {
                string genDataFile = $"{_currentDir}/{DATA_FILE_GEN}";

                using (StreamWriter sw = File.AppendText(genDataFile))
                {
                    sw.WriteLine("Genome", "Iterations", "Mean", "Standard Deviation", "Max", "Min");

                    for (int i = 0; i < count; i++)
                    {
                        EvaluationInfo res = results[i];

                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "{0},{1},{2:F4},{3:F4},{4:F4},{5:F4}", 
                            res.GenomeId, res.Iterations, res.ObjectiveFitnessMean, res.ObjectiveFitnessStandardDeviation, res.ObjectiveFitnessMax, res.ObjectiveFitnessMin));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not write to generalization file: {ex.Message}", ex);
            }

            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadKey();
        }

        private static string[] LoadGenomeFromXml()
        {
            _currentDir = ROOT_PATH;
            _dirStack.Clear();

            return Browse();
        }

        private static void TestGenomeFromXml()
        {
            string[] champions = LoadGenomeFromXml();

            int runs = GetIntegerConsoleInput("Enter number of test runs:");
            int iterations = GetIntegerConsoleInput("Enter number of iterations per run:");
            bool record = GetBoolConsoleInput("Create recordings for each run? (y/n):");
            bool generalize = GetBoolConsoleInput("Do a generalization test? (y/n):");

            _experiment.TestSavedChampion(champions[0], iterations, runs, record, generalize);
        }

        private static void StartStop()
        {
            if (!_didStartExperiment)
            {
                _didStartExperiment = true;
            }
            _experiment.StartStopEA();
        }

        private static void ToggleDebug()
        {
#if DEBUG
            ENTM.Utility.Debug.On = !ENTM.Utility.Debug.On;
            if (ENTM.Utility.Debug.On) Console.WriteLine("Debug on.");
            else Console.WriteLine("Debug off. Press D to turn Debug back on");
#else
            Console.WriteLine("Debug not available");
#endif
        }

        private static void ToggleNoveltySearch()
        {
            _experiment.NoveltySearchEnabled = !_experiment.NoveltySearchEnabled;
        }

        private static void TestCurrentChampion()
        {
            _experiment.TestCurrentChampion();
        }

        private static void TestCurrentChampionGeneralization()
        {
            _experiment.TestCurrentChampionGeneralization(1);
        }

        private static void TestCurrentPopulation()
        {
            _experiment.TestCurrentPopulation();
        }

        private static void AbortCurrentExperiment()
        {
            _experiment.AbortCurrentExperiment();
        }

        private static void AddSeedGenome()
        {
            string[] champions = LoadGenomeFromXml();
            _experiment.SeedGenome = champions[0];
            Console.WriteLine("Seed Genome loaded");
        }

        private static void ProcessInput()
        {
            do
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if (_terminated || key == ConsoleKey.Escape) break;

                InputOption.Execute(key);

            } while (true);
        }

        private delegate void InputOptionDelegate();

        protected class InputOption
        {
            private static readonly Dictionary<ConsoleKey, InputOption> Options;

            static InputOption()
            {
                Options = new Dictionary<ConsoleKey, InputOption>();

                Create(ConsoleKey.Spacebar, "Start/Pause Evolutionary Algorithm", StartStop);
                Create(ConsoleKey.D, "Toggle Debug (only available for debug builds)", ToggleDebug);
                Create(ConsoleKey.N, "Toggle Novelty Search", ToggleNoveltySearch);
                Create(ConsoleKey.C, "Test current champion", TestCurrentChampion);
                Create(ConsoleKey.G, "Test current champion generalization", TestCurrentChampionGeneralization);
                Create(ConsoleKey.P, "Test current population", TestCurrentPopulation);
                Create(ConsoleKey.S, "Test saved champion (from xml)", TestGenomeFromXml);
                Create(ConsoleKey.A, "Abort current experiment and continue with the next, if any", AbortCurrentExperiment);
                Create(ConsoleKey.E, "Add seed genome", AddSeedGenome);
            }

            protected internal static string PrintAll()
            {
                StringBuilder builder = new StringBuilder("\nControls");
                foreach (InputOption opt in Options.Values)
                {
                    builder.Append("\n").Append(opt.Readable());
                }
                return builder.Append("\n").ToString();
            }

            protected internal static bool Execute(ConsoleKey key)
            {
                if (!Options.ContainsKey(key)) return false;
                Options[key]._inputDelegate();
                return true;
            }

            private static void Create(ConsoleKey key, string description, InputOptionDelegate inputDelegate)
            {
                InputOption option = new InputOption(key, description, inputDelegate);
                Options.Add(key, option);
            }

            private InputOption(ConsoleKey key, string description, InputOptionDelegate inputDelegate)
            {
                _key = key;
                _description = description;
                _inputDelegate = inputDelegate;
            }

            private readonly ConsoleKey _key;
            private readonly string _description;
            private readonly InputOptionDelegate _inputDelegate;

            private string Readable()
            {
                return $"-{$"{_key}:",-10} {_description}";
            }
        }

        private static int GetIntegerConsoleInput(string prompt)
        {
            Console.WriteLine($"{prompt}: ");
            do
            {
                try
                {
                    return Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine($"Can't do that sir, please put in a number.");
                }
            } while (true);
        }

        private static bool GetBoolConsoleInput(string prompt) {
            Console.WriteLine($"{prompt}: ");
            do
            {
                try
                {
                    string input = Console.ReadLine();
                    input = input.ToLower();
                    if (input[0] == 'y')
                        return true;
                    if (input[0] == 'n')
                        return false;
                    Console.WriteLine($"Write n for No or y for Yes");
                }
                catch (Exception)
                {
                    Console.WriteLine($"Can't do that sir, please put in y or n.");
                }
            } while (true);
        }
    }
}
