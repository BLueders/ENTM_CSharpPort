using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM
{
    class Utilities
    {
        private static readonly IFormatProvider Provider = CultureInfo.CurrentCulture;

        public static string ToString<T>(IEnumerable<T> col)
        {
            if (col == null)
                return "null";
            if (col.Count() != 0 && col.First() is IEnumerable)
            {
                return string.Join(",", col.Select(x => ToString(x as IEnumerable<object>)).ToArray());
            }
            return string.Join(",", col.Select(x => x.ToString()).ToArray());
        }

        public static string ToString<T>(T[] col, string format) where T : IFormattable
        {
            return string.Join(",", col.Select(x => x.ToString(format, Provider)).ToArray());
        }

        public static string ToString<T>(T[][] col, string format) where T : IFormattable
        {
            return string.Join("\n", col.Select(x => ToString(x, format)).ToArray());
        }
        /**
         * Normalized manhattan distance:
         * Compares two vectors and calculates a similarity between them.
         * Only works for strictly positive numbers each between 0.0 and 1.0.
         * @param v1 the first vector
         * @param v2 the second vector
         * @return A number between 0.0 and 1.0 of how similar the two vectors
         * are (in the space of each variable being between 0.0 and 1.0).
         */
        public static double Emilarity(double[] v1, double[] v2)
        {
            if (v1.Length != v2.Length)
                throw new ArgumentException("The arrays must be of the same length");

            double numerator = v1.Select((t, i) => Math.Abs(t - v2[i])).Sum();

            return 1.0 - (numerator / v1.Length);
        }

        /**
	     * Find the index of the element with the highest value
	     * @param array The array to search through
	     * @return The index of the element with the highest value
	     */
        public static int MaxPos(double[] array)
        {
            int maxpos = 0;
            double value = double.MinValue;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > value)
                {
                    maxpos = i;
                    value = array[i];
                }
            }
            return maxpos;
        }

        public static double[][] DeepCopy(double[][] original)
        {
            if (original == null)
            {
                return null;
            }
            double[][] copy = new double[original.Length][];
            for (int i = 0; i < original.Length; i++)
            {
                copy[i] = new double[original[i].Length];
                for (int j = 0; j < original[i].Length; j++)
                {
                    copy[i][j] = original[i][j];
                }
            }
            return copy;
        }


        /// <summary>
        /// Takes a 2D array and returns the same elements in a 1D array structure.
        /// </summary>
        /// <param name="arrays">arrays The 2D array to flatten.</param>
        /// <returns>A 1D array of those arrays appended.</returns>
        public static double[] Flatten(double[][] arrays)
        {
            int offset = 0;
            double[] result = new double[TotalLength(arrays)];
            foreach (double[] a in arrays)
            {
                Array.Copy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }
            return result;
        }

        /// <summary>
        /// Count the total number of elements in a 2 dimensional matrix
        /// </summary>
        /// <param name="arrays">arrays The 2d matrix to count</param>
        /// <returns>The total number of elements in the 2d matrix</returns>
        public static int TotalLength(double[][] arrays)
        {
            return arrays.Sum(t => t.Length);
        }


        public static void ClampArray01(double[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > 1.0)
                {
                    array[i] = 1.0;
                }
            }
        }

        public static T[] ArrayCopyOfRange<T>(T[] original, int startIndex, int length)
        {
            T[] copy = new T[length];
            Array.Copy(original, startIndex, copy, 0, length);
            return copy;
        }
    }
}
