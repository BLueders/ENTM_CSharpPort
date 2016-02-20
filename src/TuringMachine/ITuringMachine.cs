using System.Dynamic;

namespace ENTM.TuringMachine
{
    interface ITuringMachine
    {
        void Reset();
        int ReadHeadCount { get; }
        int WriteHeadCount { get; }
        int InputCount { get; }
        int OutputCount { get; }
        double[][] ProcessInput(double[] input);
        double[][] GetDefaultRead();

        // Get the info saved
        double[][] TapeValues { get; }

        TuringMachineTimeStep PrevTimeStep { get; }
    }
}
