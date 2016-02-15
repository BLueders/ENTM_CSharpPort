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

namespace ENTM
{
    class Program
    {
        const string CHAMPION_FILE = "copytask_champion.xml";

        private static NeatEvolutionAlgorithm<NeatGenome> _ea;

        static void Main(string[] args)
        {
            // Initialise log4net (log to console).
            XmlConfigurator.Configure(new FileInfo("log4net.properties"));

            // Experiment classes encapsulate much of the nuts and bolts of setting up a NEAT search.
            CopyTaskExperiment experiment = new CopyTaskExperiment();

            // Load config XML.
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load("copytask.config.xml");
            experiment.Initialize("Copy Task", xmlConfig.DocumentElement);

            // Create evolution algorithm and attach update event.
            _ea = experiment.CreateEvolutionAlgorithm();
            _ea.UpdateEvent += ea_UpdateEvent;

            // Start algorithm (it will run on a background thread).
            _ea.StartContinue();

            ProcessInput();
        }

        static void ea_UpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine("gen={0:N0} bestFitness={1:N6}", _ea.CurrentGeneration, _ea.Statistics._maxFitness);

            // Save the best genome to file
            XmlDocument doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { _ea.CurrentChampGenome }, false);
            doc.Save(CHAMPION_FILE);
        }

        private static void ProcessInput()
        {
            do
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.Escape:
                        return;
                    case ConsoleKey.D:
#if DEBUG
                        Debug.On = !Debug.On;
                        if (!Debug.On)
                        {
                            //Console.SetBufferSize(0,0);
                            Console.WriteLine("Press D to turn Debug back on");
                        }
#else
                        Console.WriteLine("Debug not available");
#endif
                        break;
                }

            } while (true);
        }
    }
}
