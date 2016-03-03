using System;
using System.Xml;
using ENTM.Replay;
using SharpNeat.Core;
using SharpNeat.Domains;

namespace ENTM.Experiments
{
    interface ITuringExperiment : INeatExperiment
    {
        Recorder Recorder { get; }

        TimeSpan TimeSpent { get; }

        void Initialize(string name, XmlElement xmlConfig, string identifier, int number);

        void StartStopEA();

        FitnessInfo TestCurrentChampion();

        FitnessInfo TestSavedChampion();

        event EventHandler ExperimentStartedEvent;
        event EventHandler ExperimentCompleteEvent;

    }
}
