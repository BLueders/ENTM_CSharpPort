using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Replay;

namespace ENTM
{
    public interface IEnvironment : IReplayable<EnvironmentTimeStep>
    {
        /**
         * The Controller supervising this Simulator if
         * it has registered itself.
         */
        IController Controller { get; set; }

        /**
         * @return Number of inputs that the simulator expects
         */
        int InputCount { get; }

        /**
         * @return Number of outputs that the simulator will return
         */
        int OutputCount { get; }


        /**
         * Reset the simulator to some initial state (for new agents
         * to be tested under same circumstances
         */
        void Reset();

        /**
         * Move the agent back to start for a new round of evaluation
         * (can be different from the previous state).
         */
        void Restart();

        /**
         * Get values for the first input to the neural network
         * @return
         */
        double[] InitialObservation { get; }

        /**
         * @param action The action to simulate (must have size {@link #getInputCount()}
         * @return The output of the simulator that the input gave (will have size {@link #getOutputCount()}
         */
        double[] PerformAction(double[] action);

        /**
         * Get the current score that has been collected since the last call to {@link #reset()}
         * @return
         */
        double CurrentScore { get; }

        /**
         * @return The highest possibly obtainable score
         */
        double MaxScore { get; }

        double NormalizedScore { get; }

        /**
         * @return True in case the simulation must stop now (e.g. you won/lost the entire thing)
         */
        bool IsTerminated { get; }

        int TotalTimeSteps { get; }
    }
}
