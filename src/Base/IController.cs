using ENTM.NoveltySearch;
using SharpNeat.Phenomes;

namespace ENTM.Base
{
    public interface IController
    {
        /// <summary>
        /// The phenome that the controller is controlling
        /// </summary>
        IBlackBox Phenome { get; set; }

        /// <summary>
        /// Reset the internal state of the controller. This is called in the beginning of each evaluation iteration.
        /// </summary>
        void Reset();

        /// <summary>
        /// Activate the neural network and other controller components with the output from the environment
        /// </summary>
        /// <param name="enviromentInput"></param>
        /// <returns></returns>
        double[] ActivateNeuralNetwork(double[] environmentOutput);

        /// <summary>
        /// Novelty Search data
        /// </summary>
        NoveltySearchInfo NoveltySearch { get; set; }
    }
}