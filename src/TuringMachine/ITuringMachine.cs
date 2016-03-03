using System.Dynamic;
using ENTM.Replay;

namespace ENTM.TuringMachine
{
    public interface ITuringMachine : IReplayable<TuringMachineTimeStep>
    {
        void Reset();
        int ReadHeadCount { get; }
        int WriteHeadCount { get; }
        int InputCount { get; }
        int OutputCount { get; }
        double[][] ProcessInput(double[] input);
        double[][] GetDefaultRead();

        // Get an internal state vector to determine a novelty score
        double[] NoveltyVector { get; }

        // Get the info saved
        double[][] TapeValues { get; }
    }
}
