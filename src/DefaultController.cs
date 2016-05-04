﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.NoveltySearch;
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

            if (!Phenome.IsStateValid)
            {
                Console.WriteLine("Invalid state");
            }


            double[] nnOutput = new double[Phenome.OutputSignalArray.Length];
            Phenome.OutputSignalArray.CopyTo(nnOutput, 0);

            return nnOutput;
        }

        public NoveltySearchInfo NoveltySearch { get; set; }
    }
}