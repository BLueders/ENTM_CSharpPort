using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        public const string ROOT_PATH = "ENTM/";
        private const string CONFIG_PATH = ROOT_PATH + "Config/";
        private const string LOG4NET_CONFIG = "log4net.properties";

        private static Stopwatch _stopwatch;

        private static string _currentDir;
        private static Stack<string> _dirStack = new Stack<string>();

        private static bool _terminated = false;
        private static Type _experimentType;
        private static  XmlElement[] _configs;
        private static ITuringExperiment _experiment;
        private static readonly string _identifier = DateTime.Now.ToString("MMddyyyy-HHmmss");

        private static int _currentConfig = 0;
        private static int _currentExperiment = 1;
        private static int _experiementCount;

        public static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;


        static void Main(string[] args)
        {
            XmlConfigurator.Configure(new FileInfo(LOG4NET_CONFIG));

            Console.WriteLine("Select config");

            Console.WriteLine($"A: Execute all experiments in the current directory serially");

            _currentDir = CONFIG_PATH;
            _dirStack.Push(ROOT_PATH);

            string[] configs = Browse();

            LoadExperiments(configs);

            InitializeExperiment(_configs[0]);
            

            Console.WriteLine($"\nControls:" +
                $"\n-{"Space:", -10} Start/Pause Evolutionary Algorithm" +
                $"\n-{"D:", -10} Toggle Debug (only available for debug builds)" +
                $"\n-{"C:", -10} Test current champion" +
                $"\n-{"S:", -10} Test saved champion (from xml)" +
                $"\n-{"A:", -10} Abort current experiment and continue with the next, if any" +
                $"\n-{"Esc:", -10} Exit");

            // Start listening for input
            ProcessInput();
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
                xml.Load(configPaths[i]);

                _configs[i] = xml.DocumentElement;
            }
        }

        private static void InitializeExperiment(XmlElement config)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            _experimentType = assembly.GetType(XmlUtils.GetValueAsString(config, "ExperimentClass"), false, false);
            _experiementCount = XmlUtils.TryGetValueAsInt(config, "ExperimentCount") ?? 1;

            _experiment = (ITuringExperiment) Activator.CreateInstance(_experimentType);

            // Register event listeners
            _experiment.ExperimentStartedEvent += ExperimentStartedEvent;
            _experiment.ExperimentPausedEvent += ExperimentPausedEvent;
            _experiment.ExperimentResumedEvent += ExperimentResumedEvent;
            _experiment.ExperimentCompleteEvent += ExperimentCompleteEvent;

            _experiment.Initialize(XmlUtils.GetValueAsString(config, "Name"), config, _identifier, _currentConfig, _currentExperiment);
        }

        private static void ExperimentStartedEvent(object sender, EventArgs e)
        {
            logger.Info($"Started experiment {_experiment.Name} {_currentExperiment}");
        }

        private static void ExperimentPausedEvent(object sender, EventArgs e)
        {
            logger.Info($"Paused experiment {_experiment.Name} {_currentExperiment}");
            _stopwatch.Stop();
        }

        private static void ExperimentResumedEvent(object sender, EventArgs e)
        {
            logger.Info($"Resumed experiment {_experiment.Name} {_currentExperiment}");
            _stopwatch.Start();
        }

        private static void ExperimentCompleteEvent(object sender, EventArgs e)
        {
            logger.Info($"Time spent: {Utilities.TimeSpanToString(_experiment.TimeSpent)}");

            _experiment.TestCurrentChampion();

            _currentExperiment++;

            if (_currentExperiment > _experiementCount)
            {
                _currentConfig++;
                if (_currentConfig < _configs.Length)
                {
                    _currentExperiment = 1;
                }
                else
                {
                    _terminated = true;
                    logger.Info($"All experiments completed. Total time spent: {Utilities.TimeSpanToString(_stopwatch.Elapsed)}");
                    Console.WriteLine("\nPress any key to exit...");
                }
            }

            if (!_terminated)
            {
                InitializeExperiment(_configs[_currentConfig]);
                _experiment.StartStopEA();
            }
        }

        private static void LoadGenomeFromXml()
        {
            _currentDir = ROOT_PATH;
            _dirStack.Clear();

            string[] champions = Browse();
            _experiment.TestSavedChampion(champions[0]);
        }

        private static void ProcessInput()
        {
            do
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if (_terminated) break;

                switch (key)
                {
                    case ConsoleKey.Spacebar:
                        if (_stopwatch == null) _stopwatch = new Stopwatch();
                        _stopwatch.Start();
                        _experiment.StartStopEA();
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
                        _experiment.TestCurrentChampion();
                        break;

                    case ConsoleKey.S:
                        LoadGenomeFromXml();
                        break;

                    case ConsoleKey.A:
                        _experiment.AbortCurrentExperiment();
                        break;

                    case ConsoleKey.Escape:
                        return;
                }

            } while (true);
        }
    }
}
