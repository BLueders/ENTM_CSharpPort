using System;
using System.Threading;

namespace ENTM.Utility
{
    public static class ThreadSafeRandom
    {
        private static readonly ThreadLocal<Random> Rand = new ThreadLocal<Random>(() => new Random(GetSeed()));

        public static int Next()
        {
            return Rand.Value.Next();
        }

        public static int Next(int min, int max)
        {
            return Rand.Value.Next(min, max);
        }

        public static double NextDouble()
        {
            return Rand.Value.NextDouble();
        }

        static int GetSeed()
        {
            return 0;
            //return Environment.TickCount * Thread.CurrentThread.ManagedThreadId;
        }
    }
}
