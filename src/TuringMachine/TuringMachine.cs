

// #define DEBUG

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace ENTM.TuringMachine
{

    /**
     * Our simplified version of a Turing Machine for
     * use with a neural network. It uses the general
     * TuringMachine interface so can be used in the
     * same contexts as the GravesTuringMachine.
     * 
     * @author Emil
     *
     */
    class TuringMachine : ITuringMachine
    {
        private List<double[]> Tape;
        private int[] _headPositions;
        private int _m;
        private int _n;
        private int _shiftLength;
        private string _shiftMode;
        private bool _enabled;
        private int _heads;

        private bool _recordTimeSteps = false;
        private TuringMachineTimeStep _lastTimeStep;
        private TuringMachineTimeStep _internalLastTimeStep;
        private bool _increasedSizeDown = false;
        private int _zeroPosition = 0;

        private double[][] _initialRead;

        public TuringMachine(Properties props)
        {
            _m = props.GetIntProperty("tm.m");
            _n = props.GetIntProperty("tm.n", -1);
            _shiftLength = props.GetIntProperty("tm.shift.length");
            _shiftMode = props.GetProperty("tm.shift.mode", "multiple");
            _enabled = props.GetBooleanProperty("tm.enabled", true);
            _heads = props.GetIntProperty("tm.heads.readwrite", 1);

            Tape = new List<double[]>();

            Reset();
            _initialRead = new double[_heads][];
            for (int i = 0; i < _heads; i++)
            {
                _initialRead[i] = GetRead(i);
            }
        }

        public void Reset()
        {
            Tape.Clear();
            Tape.Add(new double[_m]);
            _headPositions = new int[_heads];

            if (_recordTimeSteps)
            {
                _internalLastTimeStep = new TuringMachineTimeStep(new double[_m], 0, 0, new double[_shiftLength], new double[_m], 0, 0, 0, 0, 0, 0);
                _lastTimeStep = new TuringMachineTimeStep(new double[_m], 0, 0, new double[_shiftLength], new double[_m], 0, 0, 0, 0, 0, 0);
            }
            if (Debug.On)
            {
                PrintState();
            }
        }

        /**
         * Operation order:
         * 	write
         *	jump
         *	shift
         *	read
         */
        public double[][] ProcessInput(double[] fromNN)
        {
            if (!_enabled)
                return _initialRead;

            double[][] result = new double[_heads][];

            double[][] writeKeys = new double[_heads][];
            double[] interps = new double[_heads];
            double[] contents = new double[_heads];
            double[][] shifts = new double[_heads][];

            int p = 0;
            // First all writes
            for (int i = 0; i < _heads; i++)
            {

                // Should be M + 2 + S elements
                writeKeys[i] = Take(fromNN, p, _m);
                p += _m;

                interps[i] = fromNN[p];
                p++;

                contents[i] = fromNN[p];
                p++;

                int s = GetShiftInputs();
                shifts[i] = Take(fromNN, p, s);
                p += s;

                if (Debug.On)
                {
                    Console.WriteLine("------------------- MINIMAL TURING MACHINE (HEAD " + (i + 1) + ") -------------------");
                    Console.WriteLine("Write=" + Utilities.ToString(writeKeys[i], "F4") + " Interp=" + interps[i]);
                    Console.WriteLine("Content?=" + contents[i] + " Shift=" + Utilities.ToString(shifts[i], "F4"));
                }

                Write(i, writeKeys[i], interps[i]);
            }

            // Perform content jump
            for (int i = 0; i < _heads; i++)
            {
                PerformContentJump(i, contents[i], writeKeys[i]);
            }

            // Shift and read (no interaction)
            for (int i = 0; i < _heads; i++)
            {
                int writePosition = _headPositions[i];
                _increasedSizeDown = false;
                MoveHead(i, shifts[i]);

                double[] headResult = GetRead(i); // Show me what you've got! \cite{rickEtAl2014}
                result[i] = headResult;

                if (_recordTimeSteps)
                {
                    int readPosition = _headPositions[i];
                    int correctedWritePosition = writePosition - _zeroPosition;

                    if (_increasedSizeDown)
                    {
                        writePosition++;
                        _zeroPosition++;
                    }
                    int correctedReadPosition = readPosition - _zeroPosition;
                    _lastTimeStep = new TuringMachineTimeStep(writeKeys[i], interps[i], contents[i], shifts[i], headResult, writePosition, readPosition, _zeroPosition, _zeroPosition, correctedWritePosition, correctedReadPosition);
                    //				                                               (double[] key, double write, double jump, double[] shift, double[] read, int writePosition, int readPosition, int writeZeroPosition, int readZeroPosition  , int correctedWritePosition, int correctedReadPosition){

                    //				correctedReadPosition = readPosition - zeroPosition;
                    _lastTimeStep = new TuringMachineTimeStep(writeKeys[i], interps[i], contents[i], shifts[i], headResult, writePosition, readPosition, _zeroPosition, _zeroPosition, correctedWritePosition, correctedReadPosition);
                    _internalLastTimeStep = new TuringMachineTimeStep(writeKeys[i], interps[i], contents[i], shifts[i], headResult, writePosition, readPosition, _zeroPosition, _zeroPosition, correctedWritePosition, correctedReadPosition);
                }
            }

            if (Debug.On)
            {

                PrintState();
                Console.WriteLine("Sending to NN: " + Utilities.ToString(result, "F4"));
                Console.WriteLine("--------------------------------------------------------------");

            }
            //		return new double[1][result.length];
            return result;
        }

        public TuringMachineTimeStep LastTimeStep => _lastTimeStep;

        public bool RecordTimeSteps
        {
            get { return _recordTimeSteps; }
            set { _recordTimeSteps = value; }
        }

        public TuringMachineTimeStep InitialTimeStep
        {
            get { return _lastTimeStep = new TuringMachineTimeStep(new double[_m], 0, 0, new double[_shiftLength], new double[_m], 0, 0, 0, 0, 0, 0); }
        }

        


        public double[][] GetDefaultRead()
        {
            if (Debug.On)
            {
                PrintState();
            }
            return _initialRead;
        }

        public int GetReadHeadCount()
        {
            return 1;
        }

        public int GetWriteHeadCount()
        {
            return 1;
        }

        public int GetInputCount()
        {
            // WriteKey, Interpolation, ToContentJump, Shift
            return _heads * (_m + 2 + GetShiftInputs());
        }

        public int GetOutputCount()
        {
            return _m * _heads;
        }

        public double[][] GetTapeValues()
        {
            return Utilities.DeepCopy(Tape.ToArray());
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append(Utilities.ToString(Tape));
            b.Append("\n");
            b.Append("Pointers=");
            b.Append(Utilities.ToString(_headPositions));
            return b.ToString();
        }

        // PRIVATE HELPER METHODS

        private static double[] Take(double[] values, int start, int amount)
        {
            double[] result = new double[amount];
            for (int i = 0; i < amount; i++)
                result[i] = values[i + start];
            return result;
        }

        private int GetShiftInputs()
        {
            switch (_shiftMode)
            {
                case "single": return 1;
                default: return _shiftLength;
            }
        }

        private void PrintState()
        {
            Console.Write("TM: " + Utilities.ToString(Tape) + " HeadPositions=" + Utilities.ToString(_headPositions));
            //Console.WriteLine("TM: " + Utilities.toString(Tape.toArray(new double[Tape.size()][])) + " HeadPositions=" + Arrays.toString(HeadPositions));
        }

        private void Write(int head, double[] content, double interp)
        {
            Tape[_headPositions[head]] = Interpolate(content, Tape[_headPositions[head]], interp);
        }

        private double[] Interpolate(double[] first, double[] second, double interp)
        {
            double[] result = new double[first.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = interp * first[i] + (1 - interp) * second[i];
            }
            return result;
        }

        private void PerformContentJump(int head, double contentJump, double[] key)
        {
            if (contentJump >= 0.5)
            {
                // JUMPING POINTER TO BEST MATCH
                int bestPos = 0;
                double similarity = -1d;
                for (int i = 0; i < Tape.Count; i++)
                {
                    double curSim = Utilities.Emilarity(key, Tape[i]);
                    if (Debug.On)
                    {
                        Console.WriteLine("Pos " + i + ": sim =" + curSim + (curSim > similarity ? " better" : ""));
                    }
                    if (curSim > similarity)
                    {
                        similarity = curSim;
                        bestPos = i;
                    }
                }

                if (Debug.On)
                {
                    Console.WriteLine("PERFORMING CONTENT JUMP! from " + _headPositions[head] + " to " + bestPos);
                }

                _headPositions[head] = bestPos;

            }
        }

        private void MoveHead(int head, double[] shift)
        {
            // SHIFTING
            int highest;
            switch (_shiftMode)
            {
                case "single": highest = (int)(shift[0] * _shiftLength); break; // single
                default: highest = Utilities.MaxPos(shift); break; // multiple
            }

            int offset = highest - (_shiftLength / 2);

            //		Console.WriteLine("Highest="+highest);
            //		Console.WriteLine("Offset="+offset);

            while (offset != 0)
            {
                if (offset > 0)
                {
                    if (_n > 0 && Tape.Count >= _n)
                    {
                        _headPositions[head] = 0;
                    }
                    else {
                        _headPositions[head] = _headPositions[head] + 1;

                        if (_headPositions[head] >= Tape.Count)
                        {
                            Tape.Add(new double[_m]);
                        }
                    }

                }
                else {
                    if (_n > 0 && Tape.Count >= _n)
                    {
                        _headPositions[head] = Tape.Count - 1;
                    }
                    else {
                        _headPositions[head] = _headPositions[head] - 1;
                        if (_headPositions[head] < 0)
                        {
                            Tape.Insert(0, new double[_m]);
                            _headPositions[head] = 0;

                            // Moving all other heads accordingly
                            for (int i = 0; i < _heads; i++)
                            {
                                if (i != head)
                                    _headPositions[i] = _headPositions[i] + 1;
                            }

                            _increasedSizeDown = true;
                        }
                    }

                }

                offset = offset > 0 ? offset - 1 : offset + 1; // Go closer to 0
            }
        }

        private double[] GetRead(int head)
        {
            return (double[])Tape[_headPositions[head]].Clone();
        }
    }
}
