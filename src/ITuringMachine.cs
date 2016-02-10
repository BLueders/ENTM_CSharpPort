using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM_CSharpPort
{
    interface ITuringMachine
    {
        void Reset();
        int GetReadHeadCount();
        int GetWriteHeadCount();
        int GetInputCount();
        int GetOutputCount();
        double[][] ProcessInput(double[] input);
        double[][] GetDefaultRead();

        // Get the info saved
        double[][] GetTapeValues();
    }
}
