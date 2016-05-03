using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Core;

namespace ENTM.MultiObjective
{
    public struct Behaviour<TGenome> where TGenome : class, IGenome<TGenome>
    {
        public TGenome Genome { get; }
        public FitnessInfo Score { get; }

        public double[] Objectives { get; set; }

        public Behaviour(TGenome genome, FitnessInfo score, int objectives)
        {
            Genome = genome;
            Score = score;
            Objectives = new double[objectives];
        }
    }
}
