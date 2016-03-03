using SharpNeat.Phenomes;

namespace ENTM
{
    public interface IController
    {
        void Reset();
        double[] InitialInput { get; }
        double[] ActivateNeuralNetwork(double[] enviromentInput, double[] controllerInput);
        double[] ProcessNNOutputs(double[] fromNN);
        double[] NoveltyVector { get; }
    }
}
