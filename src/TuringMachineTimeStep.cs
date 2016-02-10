using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM_CSharpPort
{
    class TuringMachineTimeStep { 

        public double[] Key;
        public double WriteInterpolation, ContentJump;
        public double[] Shift;
        public double[] Read;
        public int WritePosition;
        public int ReadPosition;
        public int WriteZeroPosition;
        public int ReadZeroPosition;
        public int CorrectedWritePosition;
        public int CorrectedReadPosition;
        public TuringMachineTimeStep(double[] key, double write, double jump, double[] shift, double[] read, int writePosition, int readPosition, int writeZeroPosition, int readZeroPosition, int correctedWritePosition, int correctedReadPosition)
        {
            Key = key;
            WriteInterpolation = write;
            ContentJump = jump;
            Shift = shift;
            Read = read;
            WritePosition = writePosition;
            ReadPosition = readPosition;
            WriteZeroPosition = writeZeroPosition;
            ReadZeroPosition = readZeroPosition;
            CorrectedWritePosition = correctedWritePosition;
            CorrectedReadPosition = correctedReadPosition;
        }
}
}
