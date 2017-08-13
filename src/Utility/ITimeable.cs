namespace ENTM.Utility
{
    public interface ITimeable
    {
        long TimeSpent { get; } 
        long TimeSpentAccumulated { get; set; }
    }
}