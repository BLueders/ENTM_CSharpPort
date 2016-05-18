using System.Collections.Generic;
using System.Dynamic;
using ENTM.NoveltySearch;
using ENTM.Replay;

namespace ENTM.TuringMachine
{
    public interface ITuringMachine : IReplayable<TuringMachineTimeStep>
    {
        int ReadHeadCount { get; }

        int WriteHeadCount { get; }

        int InputCount { get; }

        int OutputCount { get; }

        double[][] DefaultRead { get; }

        /// <summary>
        /// Novelty Search data
        /// </summary>
        NoveltySearchInfo NoveltySearch { get; set; }

        // Get the info saved
        double[][] TapeValues { get; }

        // Activates the turing machine
        double[][] ProcessInput(double[] input);

        // Reset the turing machine's internal state for a new evaluation
        void Reset();
    }
}
