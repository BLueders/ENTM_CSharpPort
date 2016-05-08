namespace ENTM.Base
{
    public struct EvaluationInfo
    {
        public double ObjectiveFitness { get; }

        public double[][] NoveltyVectors { get; }

        public double[] MinimumCriteria { get; }

        public EvaluationInfo(double objectiveFitness, double[][] noveltyVectors, double[] minimumCriteria)
        {
            ObjectiveFitness = objectiveFitness;
            NoveltyVectors = noveltyVectors;
            MinimumCriteria = minimumCriteria;
        }

        public EvaluationInfo(double fitness)
        {
            ObjectiveFitness = fitness;
            NoveltyVectors = new double[0][];
            MinimumCriteria = new double[0];
        }
    }
}