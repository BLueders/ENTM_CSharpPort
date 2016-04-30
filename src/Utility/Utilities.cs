using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ENTM.Utility
{
    public class Utilities
    {
        private static string STANDARD_FORMAT = "F3";

        private static readonly IFormatProvider Provider = CultureInfo.CurrentCulture;

        public static string ToString(IEnumerable col)
        {
            return ToString(col, STANDARD_FORMAT);
        }

        public static string ToString(IEnumerable col, string format)
        {
            var enumerator = col.GetEnumerator();
            enumerator.MoveNext();
            if (enumerator.Current == null)
                return "null";
            StringBuilder builder = new StringBuilder();
            if (enumerator.Current is IEnumerable)
            {
                do
                {
                    builder.Append("|" + ToString((IEnumerable)enumerator.Current, format) + "|\n");
                } while (enumerator.MoveNext());
                return builder.ToString();
            }
            if (enumerator.Current is IFormattable)
                do
                {
                    {
                        builder.Append(((IFormattable)enumerator.Current).ToString(format, Provider) + ", ");
                    }
                } while (enumerator.MoveNext());
            else
            {
                do
                {
                    builder.Append(enumerator.Current + ", ");
                } while (enumerator.MoveNext());
            }
            return builder.ToString();
        }

        public static string ToString<T>(T[] col, string format) where T : IFormattable
        {
            return string.Join(", ", col.Select(x => x.ToString(format, Provider)).ToArray());
        }

        public static string ToString<T>(T[][] col, string format) where T : IFormattable
        {
            return string.Join("\n", col.Select(x => ToString(x, format)).ToArray());
        }

        /// <summary>
        /// Normalized manhattan distance:
        /// Compares two vectors and calculates a similarity between them.
        /// Only works for strictly positive numbers each between 0.0 and 1.0.
        /// </summary>
        /// <param name="v1">The first vector</param>
        /// <param name="v2">The second vector</param>
        /// <returns>A number between 0.0 and 1.0 of how similar the two vectors are 
        /// (in the space of each variable being between 0.0 and 1.0).
        public static double Emilarity(double[] v1, double[] v2)
        {
            if (v1.Length != v2.Length)
                throw new ArgumentException("The arrays must be of the same length");

            double numerator = v1.Select((t, i) => Math.Abs(t - v2[i])).Sum();

            return 1.0 - (numerator / v1.Length);
        }

        /// <summary>
        /// Find the index of the element with the highest value
        /// </summary>
        /// <param name="array">The array to search through</param>
        /// <returns>The index of the element with the highest value</returns>
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

        public static T[] JoinArrays<T>(T[] array1, T[] array2)
        {
            T[] newArray = new T[array1.Length + array2.Length];
            Array.Copy(array1, newArray, array1.Length);
            Array.Copy(array2, 0, newArray, array1.Length, array2.Length);
            return newArray;
        }

        public static double Mean(double[] values)
        {
            double mean = 0;
            for (int i = 0; i < values.Length; i++)
            {
                mean += values[i];
            }
            return mean / values.Length;
        }

        public static double StandartDeviation(double[] values) {
            double mean = Mean(values);
            double sd = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sd += (values[i] - mean) * (values[i] - mean);
            }
            return Math.Sqrt(sd / values.Length);
        }

        public static string TimeToString(TimeSpan time)
        {
            return string.Format($"{time.Hours:D2}h:{time.Minutes:D2}m:{time.Seconds:D2}s:{time.Milliseconds:D3}ms");
        }

        public static string TimeToString(long time)
        {
            return TimeToString(new TimeSpan(0, 0, 0, 0, (int) time));
        }
    }
}
