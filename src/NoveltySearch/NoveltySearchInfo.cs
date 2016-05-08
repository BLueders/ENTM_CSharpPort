using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.NoveltySearch
{
    public class NoveltySearchInfo
    {
        /// <summary>
        /// Whether or not the novelty search is currently enabled
        /// </summary>
        public bool ScoreNovelty { get; set; }

        /// <summary>
        /// Novely vector mode - what type of data to use as novelty vector
        /// </summary>
        public NoveltyVectorMode VectorMode { get; set; }

        /// <summary>
        /// The dimensionality of the novelty vector
        /// </summary>
        public int NoveltyVectorDimensions { get; set; }

        /// <summary>
        /// Length of the novelty vector (usually timesteps)
        /// </summary>
        public int NoveltyVectorLength { get; set; }

        /// <summary>
        /// Length of the minimum criteria info
        /// </summary>
        public int MinimumCriteriaLength { get; set; }

        /// <summary>
        /// The novelty score vector for a given evaluation
        /// </summary>
        public double[][] NoveltyVectors { get; private set; }

        /// <summary>
        /// The minimum criteria info for a given evaluation
        /// </summary>
        public double[] MinimumCriteria { get; private set; }

        public void Reset()
        {
            NoveltyVectors = new double[NoveltyVectorLength][];
            for (int i = 0; i < NoveltyVectorLength; i++)
            {
                NoveltyVectors[i] = new double[NoveltyVectorDimensions];
            }
            MinimumCriteria = new double[MinimumCriteriaLength];
        }
    }
}
