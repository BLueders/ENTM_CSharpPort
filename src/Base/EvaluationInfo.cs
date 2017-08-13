namespace ENTM.Base
{
    public struct EvaluationInfo
    {
        // Objective fitness averaged over all evaluation iterations
        public double ObjectiveFitness;

        public double[][] NoveltyVectors;

        public double[] MinimumCriteria;

        public int[] TapeSizes;
        

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