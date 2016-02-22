using System;
using System.Diagnostics;
using System.Threading;

namespace ENTM.Utility
{
    public abstract class Debug
    {
        private static Thread _debugThread;

        public static bool On { get; set; } = true;

        [Conditional("DEBUG")]
        public static void Log(string text)
        {
            Log(text, false);
        }

        private const string HORIZONTAL_LINE = "--------------------";

        [Conditional("DEBUG")]
        public static void LogHeader(string text, bool singleThread)
        {
            Log($"{HORIZONTAL_LINE} {text} {HORIZONTAL_LINE}", singleThread);
        }

        [Conditional("DEBUG")]
        public static void Log(string text, bool singleThread)
        {
            // Do not print if we debug on a single thread and we are not on that thread
            if (!On || (singleThread && !IsDebugTread)) return;

            Console.WriteLine(text);
        }


        public static bool IsDebugTread
        {
            get
            {
                if (_debugThread == null || !_debugThread.IsAlive)
                {
                    _debugThread = Thread.CurrentThread;
                }
                
                // Return true if we are on the debug thread, or if we are on the main thread (for testing)
                return _debugThread == Thread.CurrentThread || Program.MainThreadId == Thread.CurrentThread.ManagedThreadId;
            }
        }
    }
}
