//#define GLOBALDEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ENTM
{
    public abstract class Debug
    {
        private static int? _debugThreadDennisId = null;

        public static bool On
        {
            get
            {
#if GLOBALDEBUG
                if (_debugThreadDennisId == null)
                {
                    _debugThreadDennisId = Thread.CurrentThread.ManagedThreadId;
                }

                return _debugThreadDennisId == Thread.CurrentThread.ManagedThreadId;

#else
                return false;
#endif
            }
        }


    }
}
