using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.TuringMachine
{
    public class TuringMachineTimeStep
    {

        public readonly double[] Key;
        public readonly double WriteInterpolation, ContentJump;
        public readonly double[] Shift;
        public readonly double[] Read;
        public readonly int WritePosition;
        public readonly int ReadPosition;
        public readonly int WriteZeroPosition;
        public readonly int ReadZeroPosition;
        public readonly int CorrectedWritePosition;
        public readonly int CorrectedReadPosition;

        internal TuringMachineTimeStep(double[] key, double write, double jump, double[] shift, double[] read, int writePosition, int readPosition, int writeZeroPosition, int readZeroPosition, int correctedWritePosition, int correctedReadPosition)
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
