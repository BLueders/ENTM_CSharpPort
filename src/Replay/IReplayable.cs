namespace ENTM.Replay
{
    public interface IReplayable<TTimestep>
    {
        bool RecordTimeSteps { get; set; }
        TTimestep InitialTimeStep { get; }
        TTimestep PreviousTimeStep { get; }
    }
}
