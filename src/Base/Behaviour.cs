using System.Collections.Generic;
using ENTM.Distance;
using ENTM.MultiObjective;
using SharpNeat.Core;
using EvaluationInfo = ENTM.MultiObjective.EvaluationInfo;

namespace ENTM.Base
{
    public class Behaviour<TGenome> : Knn.INeighbour, IMultiObjectiveBehaviour where TGenome : class, IGenome<TGenome>
    {
        // The genome associated with this behaviour
        public TGenome Genome { get; }

        // Information about phenome evaluation
        private EvaluationInfo _evaluation;
        public EvaluationInfo Evaluation
        {
            get { return _evaluation; }
            set
            {
                _evaluation = value;
                Objectives[0] = _evaluation.ObjectiveFitness;
            }
        }

        // The final fitness calculated by the Multi Objective algorithm (if applied)
        public double MultiObjectiveScore { get; set; }

        // The objective scores to be considered
        public double[] Objectives { get; set; }

        // The rank of this behaviour, i.e. which Pareto front is it on
        public int Rank { get; set; }

        // List of behaviours dominated by this behaviour
        public IList<IMultiObjectiveBehaviour> Dominates { get; }

        // How many behaviours dominate this behaviour
        public int DominatedCount { get; set; }

        public Behaviour(TGenome genome, int objectives)
        {
            Genome = genome;

            Objectives = new double[objectives];

            Dominates = new List<IMultiObjectiveBehaviour>();

            Reset();
        }

        public void Reset()
        {
            Rank = 0;
            DominatedCount = 0;
            Dominates.Clear();
        }
        
        // If this behaviour has been deemed non-viable
        private bool _nonViable;
        public bool NonViable
        {
            get { return _nonViable; }
            set
            {
                _nonViable = value;
                Genome.EvaluationInfo.SetFitness(0d);
            }
        }

        public void ApplyObjectiveFitnessOnly()
        {
            Genome.EvaluationInfo.SetFitness(Evaluation.ObjectiveFitness);
        }

        public void ApplyNoveltyScoreOnly()
        {
            Genome.EvaluationInfo.SetFitness(Objectives[1]);
        }

        public void ApplyMultiObjectiveScore()
        {
            Genome.EvaluationInfo.SetFitness(MultiObjectiveScore);
        }

        public double[] KnnVector => Evaluation.NoveltyVector;
    }
}
