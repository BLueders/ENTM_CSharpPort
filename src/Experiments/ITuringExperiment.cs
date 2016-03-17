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

        bool NoveltySearchEnabled { get; set; }

        void Initialize(string name, XmlElement xmlConfig, string identifier, int subIdentifier, int number);

        void StartStopEA();
    
        FitnessInfo TestCurrentChampion();

        FitnessInfo TestSavedChampion(string xmlChampion);

        void AbortCurrentExperiment();

        event EventHandler ExperimentStartedEvent;
        event EventHandler ExperimentPausedEvent;
        event EventHandler ExperimentResumedEvent;
        event EventHandler ExperimentCompleteEvent;

    }
}
