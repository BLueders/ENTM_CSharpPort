using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.TuringMachine
{
    public struct TuringMachineTimeStep
    {

        public readonly double[] Key;
        public readonly double WriteInterpolation;
        public readonly double ContentJump;
        public readonly double[] Shift;
        public readonly double[] Read;
        public readonly double[] Written;
        public readonly int WritePosition;
        public readonly int ReadPosition;
        public readonly int ZeroPosition;
        public readonly int CorrectedWritePosition;
        public readonly int CorrectedReadPosition;
        public readonly int MemorySize;

        internal TuringMachineTimeStep(double[] key, double writeInterpolation, double contentJump, double[] shift, double[] read, double[] written,
            int writePosition, int readPosition, int zeroPosition, int correctedWritePosition, int correctedReadPosition, int memorySize)
        {
            Key = key;
            WriteInterpolation = writeInterpolation;
            ContentJump = contentJump;
            Shift = shift;
            Read = read;
            Written = written;
            WritePosition = writePosition;
            ReadPosition = readPosition;
            ZeroPosition = zeroPosition;
            CorrectedWritePosition = correctedWritePosition;
            CorrectedReadPosition = correctedReadPosition;
            MemorySize = memorySize;
        }
    }
}
