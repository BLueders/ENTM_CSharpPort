using System.Collections.Generic;
using System.Text;
using ENTM.Distance;
using ENTM.MultiObjective;
using SharpNeat.Core;

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

        public Behaviour(TGenome genome, int objectives)
        {
            Genome = genome;

            Objectives = new double[objectives];

            Reset();
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

        #region Knn.INeighbour

        public double[][] KnnVectors => Evaluation.NoveltyVectors;

        public double[] KnnVector => null;

        #endregion


        #region IMultiObjectiveBehaviour

        public double MultiObjectiveScore { get; set; }
        public double[] Objectives { get; set; }
        public int Rank { get; set; }
        public double CrowdingDistance { get; set; }
        public IList<IMultiObjectiveBehaviour> Dominates { get; set; }
        public int DominatedCount { get; set; }

        public void Reject()
        {
            NonViable = true;
        }

        public void Reset()
        {
            Dominates = new List<IMultiObjectiveBehaviour>();
            Rank = 0;
            CrowdingDistance = 0;
            DominatedCount = 0;
        }

        #endregion

        public override string ToString()
        {
            StringBuilder s = new StringBuilder($"ID: {Genome.Id} Rnk: {Rank} Scr: {MultiObjectiveScore:F04} Doms: {Dominates.Count} Domd: {DominatedCount} CrwDist: {CrowdingDistance:F04} Obj:");

            for (int i = 0; i < Objectives.Length; i++)
            {
                s.Append($" [{i}]: {Objectives[i]:F04}");
            }
            return s.ToString();
        }
    }
}
