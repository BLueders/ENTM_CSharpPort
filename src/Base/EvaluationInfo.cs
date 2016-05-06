namespace ENTM.Base
{
    public struct EvaluationInfo
    {
        public double ObjectiveFitness { get; }

        public double[] NoveltyVector { get; }

        public double[] MinimumCriteria { get; }

        public EvaluationInfo(double objectiveFitness, double[] noveltyVector, double[] minimumCriteria)
        {
            ObjectiveFitness = objectiveFitness;
            NoveltyVector = noveltyVector;
            MinimumCriteria = minimumCriteria;
        }

        public EvaluationInfo(double fitness)
        {
            ObjectiveFitness = fitness;
            NoveltyVector = new double[0];
            MinimumCriteria = new double[0];
        }
    }
}