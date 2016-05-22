namespace ENTM.Base
{
    public struct EvaluationInfo
    {
        public double ObjectiveFitness;

        public double[][] NoveltyVectors;

        public double[] MinimumCriteria;



        /// Only used for testing

        public uint GenomeId;

        public int Iterations;

        public double[] ObjectiveFitnessIt;

        public double ObjectiveFitnessMean;

        public double ObjectiveFitnessStandardDeviation;

        public double ObjectiveFitnessMin;

        public double ObjectiveFitnessMax;
    }
}