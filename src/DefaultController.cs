using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Phenomes;

namespace ENTM
{
    public class DefaultController : IController
    {
        public IBlackBox Phenome { get; set; }
        public void Reset()
        {
        }

        public double[] ActivateNeuralNetwork(double[] environmentOutput)
        {
            // Activate the neural network
            Phenome.ResetState();
            Phenome.InputSignalArray.CopyFrom(environmentOutput, 0);
            Phenome.Activate();

            double[] nnOutput = new double[Phenome.OutputSignalArray.Length];
            Phenome.OutputSignalArray.CopyTo(nnOutput, 0);

            return nnOutput;
        }

        public bool ScoreNovelty { get; set; }
        public int NoveltyVectorLength { get; set; }
        public double[] NoveltyVector { get; }
    }
}