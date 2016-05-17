using ENTM.NoveltySearch;
using ENTM.Replay;

namespace ENTM.Base
{
    public interface IEnvironment : IReplayable<EnvironmentTimeStep>
    {
        /// <summary>
        /// The Controller supervising this Simulator if it has registered itself.
        /// </summary>
        IController Controller { get; set; }

        /// <summary>
        /// Number of inputs that the environment expects
        /// </summary>
        int InputCount { get; }

        /// <summary>
        /// Number of outputs that the simulator will return
        /// </summary>
        int OutputCount { get; }

        /// <summary>
        /// Get values for the first input to the neural network
        /// </summary>
        double[] InitialObservation { get; }

        /// <summary>
        /// Get the current score that has been collected since the last call to Reset
        /// </summary>
        double CurrentScore { get; }

        /// <summary>
        /// The highest possibly obtainable score
        /// </summary>
        double MaxScore { get; }

        /// <summary>
        /// The current score normalized between 0 and 1 relative to MaxScore
        /// </summary>
        double NormalizedScore { get; }

        /// <summary>
        /// True when the task is over (all time steps have been completed)
        /// </summary>
        bool IsTerminated { get; }

        /// <summary>
        /// Total number of time steps for this environment
        /// </summary>
        int TotalTimeSteps { get; }

        /// <summary>
        /// Maximum possible timesteps if random
        /// </summary>
        int MaxTimeSteps { get; }

        /// <summary>
        /// Novelty Search data
        /// </summary>
        NoveltySearchInfo NoveltySearch { get; set; }

        int NoveltyVectorLength { get; }

        int NoveltyVectorDimensions { get; }

        int MinimumCriteriaLength { get; }

        /// <summary>
        /// ResetAll the simulator to some initial state (for new agents to be tested under same circumstances
        /// </summary>
        void ResetAll();

        /// <summary>
        /// Move the agent back to start for a new round of evaluation (can be different from the previous state).
        /// </summary>
        void ResetIteration();

        /// <summary>
        /// <param name="action">The action to simulate(must have size InputCount</param>
        /// <returns>The output of the simulator that the input gave(will have size OutputCount</returns>
        /// </summary>
        double[] PerformAction(double[] action);
    }
}
