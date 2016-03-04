using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using ENTM.Utility;
using log4net.Config;
using SharpNeat.Domains;
using ENTM.Experiments;
using System.Threading;

namespace ENTM
{
    class Program
    {
        const string CONFIG_PATH = "Config/";

        private static bool _terminated = false;
        private static Type _experimentType;
        private static  XmlElement _config;
        private static ITuringExperiment _experiment;
        private static string _identifier = DateTime.Now.ToString("MMddyyyy-HHmmss");
        private static int _currentExperiment = 0;
        private static int _experiementCount;

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

            InitializeExperiment();

            Console.WriteLine($"Controls:" +
                $"\n-{"Space:", -10} Start/Pause Evolutionary Algorithm" +
                $"\n-{"D:", -10} Toggle Debug (only available for debug builds)" +
                $"\n-{"C:", -10} Test current champion" +
                $"\n-{"S:", -10} Test saved champion (from champion.xml)" +
                $"\n-{"A:", -10} Abort current experiment and continue with the next, if any" +
                $"\n-{"Esc:", -10} Exit");

            // Start listening for input
            ProcessInput();
        }

        private static List<string> GetConfigs()
        {
            return Directory.EnumerateFiles(CONFIG_PATH, "*.xml").ToList();
        }

        private static void LoadExperiment(string configPath)
        {
            // Load config XML.
            XmlDocument xml = new XmlDocument();
            xml.Load(configPath);

            _config = xml.DocumentElement;

            Assembly assembly = Assembly.GetExecutingAssembly();

            _experimentType = assembly.GetType(XmlUtils.GetValueAsString(_config, "ExperimentClass"), false, false);
            _experiementCount = XmlUtils.TryGetValueAsInt(_config, "ExperimentCount") ?? 1;
        }

        private static void InitializeExperiment()
        {
            _currentExperiment++;

            _experiment = (ITuringExperiment) Activator.CreateInstance(_experimentType);
            _experiment.ExperimentStartedEvent += ExperimentStartedEvent;
            _experiment.ExperimentCompleteEvent += ExperimentCompleteEvent;

            _experiment.Initialize(XmlUtils.GetValueAsString(_config, "Name"), _config, _identifier, _currentExperiment);
        }

        private static void ExperimentStartedEvent(object sender, EventArgs e)
        {
            Console.WriteLine($"Started experiment {_currentExperiment}");
        }

        private static void ExperimentCompleteEvent(object sender, EventArgs e)
        {
            Console.WriteLine($"Time spent: {Utilities.TimeSpanToString(_experiment.TimeSpent)}");

            _experiment.TestCurrentChampion();

            if (_currentExperiment < _experiementCount)
            {

                InitializeExperiment();
                _experiment.StartStopEA();
            }
            else
            {
                _terminated = true;
                Console.WriteLine("All experiments completed. Press any key to exit...");
            }
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
                        _experiment.TestSavedChampion();
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
