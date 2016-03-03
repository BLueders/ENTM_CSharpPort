using SharpNeat.Phenomes;

namespace ENTM
{
    public interface IController
    {
        void Reset();
        double[] ActivateNeuralNetwork(double[] enviromentInput);
        double[] NoveltyVector { get; }
    }
}
