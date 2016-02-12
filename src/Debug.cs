#define GLOBALDEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM
{
    public abstract class Debug
    {
        public static bool On =>
        
#if GLOBALDEBUG
            true;
#else
            false;
#endif
    }
}
