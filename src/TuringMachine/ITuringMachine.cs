namespace ENTM.TuringMachine
{
    interface ITuringMachine
    {
        void Reset();
        int GetReadHeadCount();
        int GetWriteHeadCount();
        int GetInputCount();
        int GetOutputCount();
        double[][] ProcessInput(double[] input);
        double[][] GetDefaultRead();

        // Get the info saved
        double[][] GetTapeValues();

        TuringMachineTimeStep LastTimeStep { get; }
    }
}
