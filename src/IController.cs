using System.Collections.Generic;
using SharpNeat.Phenomes;

namespace ENTM
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
        /// Whether or not the controller should enable novelty search scoring
        /// </summary>
        bool ScoreNovelty { get; set; }

        /// <summary>
        /// Length of the novelty vector
        /// </summary>
        int NoveltyVectorLength { get; set; }

        /// <summary>
        /// Return the novelty score vector for a given evaluation
        /// </summary>
        double[] NoveltyVector { get; }
    }
}
