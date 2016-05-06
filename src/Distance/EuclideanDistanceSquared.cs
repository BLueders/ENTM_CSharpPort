namespace ENTM.Distance
{
    public class EuclideanDistanceSquared : IDistanceMeasure
    {
        public double Distance(double[] x, double[] y)
        {
            return Distance(x, y, x.Length, y.Length);
        }

        public double Distance(double[] x, double[] y, int lx, int ly)
        {
            // Longest vector
            int length = lx > ly ? lx : ly;

            double distance = 0;
            for (int i = 0; i < length; i++)
            {
                // Fill with zeroes if vector lenghts are not equal
                double vx = i < lx ? x[i] : 0d;
                double vy = i < ly ? y[i] : 0d;

                double d = vy - vx;
                distance += d * d;
            }

            return distance;
        }
    }
}