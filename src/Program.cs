using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using log4net;

namespace ENTM
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        public const string ROOT_PATH = "ENTM/";
        private const string CONFIG_PATH = ROOT_PATH + "Config/";
        private const string LOG4NET_CONFIG = "log4net.properties";

        private static Stopwatch _stopwatch = new Stopwatch();

        private static string _currentDir;
        private static Stack<string> _dirStack = new Stack<string>();

        private static bool _terminated = false;
        private static Type _experimentType;
        private static  XmlElement[] _configs;
        private static ITuringExperiment _experiment;
        private static readonly string _identifier = DateTime.Now.ToString("MMddyyyy-HHmmss");

        private static int _currentConfig = -1;
        private static int _currentExperiment;
        private static int _experiementCount;

        public static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

        static void Main(string[] args)
        {
            string[] configs;

            if (args.Length > 0)
            {
               configs = ParseArgs(args);
            }
            else
            {
                Console.WriteLine("No args found, prompting...");
                configs = Prompt();
            }

            LoadExperiments(configs);

            PrintOptions();

            NextExperiment();

            // Start listening for input from console
            ProcessInput();
        }

        private static string[] ParseArgs(string[] args)
        {
            string[] configs = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                string config = args[i];
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
            Console.WriteLine("Select config");

            Console.WriteLine($"A: Execute all experiments in the current directory serially");

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
            _configs = new XmlElement[configPaths.Length];

            for (int i = 0; i < configPaths.Length; i++)
            {
                // Load config XML.
                XmlDocument xml = new XmlDocument();

                try
                {
                    xml.Load(configPaths[i]);
                    _configs[i] = xml.DocumentElement;

                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"File not found: {configPaths[i]}");
                }
            }
        }

        private static bool InitializeExperiment(XmlElement config)
        {
            if (config == null)
            {
                Console.WriteLine("Config was null, aborting...");
                return false;
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            _experimentType = assembly.GetType(XmlUtils.GetValueAsString(config, "ExperimentClass"), false, false);
            _experiementCount = XmlUtils.TryGetValueAsInt(config, "ExperimentCount") ?? 1;

            _experiment = (ITuringExperiment) Activator.CreateInstance(_experimentType);

            string name = XmlUtils.GetValueAsString(config, "Name");

            // Initialize logging
            GlobalContext.Properties["name"] = name;
            GlobalContext.Properties["id"] = _identifier;
            GlobalContext.Properties["count"] = _currentConfig;
            XmlConfigurator.Configure(new FileInfo(LOG4NET_CONFIG));

            _logger.Info($"Initializing experiment: {name}...");

            // Register event listeners
            _experiment.ExperimentStartedEvent += ExperimentStartedEvent;
            _experiment.ExperimentPausedEvent += ExperimentPausedEvent;
            _experiment.ExperimentResumedEvent += ExperimentResumedEvent;
            _experiment.ExperimentCompleteEvent += ExperimentCompleteEvent;

            _experiment.Initialize(name, config, _identifier, _currentConfig, _currentExperiment);

            return true;
        }

        private static void ExperimentStartedEvent(object sender, EventArgs e)
        {
            _logger.Info($"Started experiment {_experiment.Name} {_currentExperiment}/{_experiementCount}");
            ConfigPrinter.Print(_configs[_currentConfig]);
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

        private static void ExperimentCompleteEvent(object sender, EventArgs e)
        {
            _logger.Info($"Time spent: {Utilities.TimeToString(_experiment.TimeSpent)}");

            _experiment.TestCurrentChampion();

            NextExperiment();
        }

        private static void NextExperiment() {

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
                    _experiment.StartStopEA();
                }
                else
                {
                    NextConfig();
                }
            }
        }

        private static void NextConfig()
        {
            if (_currentConfig < _configs.Length)
            {
                _currentConfig++;
                _currentExperiment = 1;
            }
            else
            {
                _terminated = true;
                _logger.Info($"All experiments completed. Total time spent: {Utilities.TimeToString(_stopwatch.Elapsed)}");
                Console.WriteLine("\nPress any key to exit...");
            }
        }

        private static void LoadGenomeFromXml()
        {
            _currentDir = ROOT_PATH;
            _dirStack.Clear();

            string[] champions = Browse();
            _experiment.TestSavedChampion(champions[0]);
        }

        private static void StartStop()
        {
            _stopwatch.Start();
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

        private static void AbortCurrentExperiment()
        {
            _experiment.AbortCurrentExperiment();
        }

        private static void ProcessInput()
        {
            do
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if (_terminated || key == ConsoleKey.Escape) break;

                if (!InputOption.Execute(key))
                {
                    Console.WriteLine("Unrecognized command. Yikes.");
                }

            } while (true);
        }

        private delegate void InputOptionDelegate();

        protected class InputOption
        {
            private static readonly Dictionary<ConsoleKey, InputOption> Options;

            static InputOption()
            {
                Options = new Dictionary<ConsoleKey, InputOption>();

                new InputOption(ConsoleKey.Spacebar, "Start/Pause Evolutionary Algorithm", StartStop);
                new InputOption(ConsoleKey.D, "Toggle Debug (only available for debug builds)", ToggleDebug);
                new InputOption(ConsoleKey.N, "Toggle Novelty Search", ToggleNoveltySearch);
                new InputOption(ConsoleKey.C, "Test current champion", TestCurrentChampion);
                new InputOption(ConsoleKey.S, "Test saved champion (from xml)", LoadGenomeFromXml);
                new InputOption(ConsoleKey.A, "Abort current experiment and continue with the next, if any", AbortCurrentExperiment);
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
                Options[key].InputDelegate();
                return true;
            }

            private InputOption(ConsoleKey key, string description, InputOptionDelegate inputDelegate)
            {
                Key = key;
                Description = description;
                InputDelegate = inputDelegate;

                Options.Add(Key, this);
            }

            private readonly ConsoleKey Key;
            private readonly string Description;
            private readonly InputOptionDelegate InputDelegate;

            private string Readable()
            {
                return $"-{$"{Key}:",-10} {Description}";
            }
        }
    }
}
