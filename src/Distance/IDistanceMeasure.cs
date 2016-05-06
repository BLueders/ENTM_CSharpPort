namespace ENTM.Distance
{
    public interface IDistanceMeasure
    {
        /// <summary>
        /// Calculate the distance between vector x and vector y according to the distance measure.
        /// </summary>
        /// <param name="x">Vector x</param>
        /// <param name="y">Vector y</param>
        /// <returns>The distance</returns>
        double Distance(double[] x, double[] y);

        /// <summary>
        /// Calculate the distance between vector x and vector y according to the distance measure.
        /// Use this overload for precalculated vector lengths, to minimize time. Vectors can be of unequal length.
        /// In this case, the short vector should be zero padded.
        /// </summary>
        /// <param name="x">Vector x</param>
        /// <param name="y">Vector y</param>
        /// <param name="lx">Length of Vector x</param>
        /// <param name="ly">Length of Vector y</param>
        /// <returns>The distance</returns>
        double Distance(double[] x, double[] y, int lx, int ly);
    }
}