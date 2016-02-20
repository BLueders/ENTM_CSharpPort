
// #define DEBUG

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using ENTM.Experiments.CopyTask;
using ENTM.Utility;

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
    public class MinimalTuringMachine : ITuringMachine
    {
        private readonly List<double[]> _tape;
        private int[] _headPositions;

        // The length of the memory at each location.
        private readonly int _m;

        // The number of memory locations in the FINITE tape.
        private readonly int _n;

        // The maximum distance you can jump with shifting.
        private readonly int _shiftLength;

        // Single or multiple shifts
        private readonly ShiftMode _shiftMode;

        // If false, the turing machine always returns initial read
        private readonly bool _enabled;

        // Number of combined read/write heads
        private readonly int _heads;

        private bool _recordTimeSteps = false;
        private TuringMachineTimeStep _lastTimeStep;
        private TuringMachineTimeStep _internalLastTimeStep;
        private bool _increasedSizeDown = false;
        private int _zeroPosition = 0;

        private double[][] _initialRead;

        // Number of times each location was accessed during livetime of the tm
        private List<int> _writeActivities = new List<int>();
        private List<int> _readActivities = new List<int>();

        public MinimalTuringMachine(TuringMachineProperties props)
        {
            _m = props.M;
            _n = props.N;
            _shiftLength = props.ShiftLength;
            _shiftMode = props.ShiftMode;
            _enabled = props.Enabled;
            _heads = props.Heads;

            _tape = new List<double[]>();

            Reset();
            _initialRead = new double[_heads][];
            for (int i = 0; i < _heads; i++)
            {
                _initialRead[i] = GetRead(i);
            }
            
        }

        public void Reset()
        {
            _tape.Clear();
            _tape.Add(new double[_m]);
            _headPositions = new int[_heads];

            if (_recordTimeSteps)
            {
                _internalLastTimeStep = new TuringMachineTimeStep(new double[_m], 0, 0, new double[_shiftLength], new double[_m], 0, 0, 0, 0, 0, 0);
                _lastTimeStep = new TuringMachineTimeStep(new double[_m], 0, 0, new double[_shiftLength], new double[_m], 0, 0, 0, 0, 0, 0);
            }

            // clear debug log lists
            _readActivities.Clear();
            _readActivities.Add(0);
            _writeActivities.Clear();
            _writeActivities.Add(0);

            Debug.Log(PrintState(), true);
        }

        /// <summary>
        /// Activate the Turing machine
        /// Operation order:
        ///	write
        /// jump
        /// shift
        /// read
        /// </summary>
        /// <param name="fromNN">Turing machine input (NN output)</param>
        /// <returns>Turing machine output (NN input)</returns>

        public double[][] ProcessInput(double[] fromNN)
        {
            Debug.LogHeader("MINIMAL TURING MACHINE START", true);
            Debug.Log($"From NN: {Utilities.ToString(fromNN, "f4")}", true);

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

                Debug.LogHeader($"HEAD {i + 1}", true);
                Debug.Log($"\nWrite:        \t{Utilities.ToString(writeKeys[i], "f4")}" +
                          $"\nInterpolate:  \t{interps[i].ToString("f4")}" +
                          $"\nContent:      \t{contents[i].ToString("f4")}" +
                          $"\nShift:        \t{Utilities.ToString(shifts[i], "f4")}", true);

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

            Debug.Log(PrintState(), true);
            Debug.Log("Sending to NN: " + Utilities.ToString(result, "F4"), true);
            Debug.LogHeader("MINIMAL TURING MACHINE END", true);
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
            Debug.Log(PrintState(), true);
            return _initialRead;
        }

        public int ReadHeadCount => 1;

        public int WriteHeadCount => 1;

        // WriteKey, Interpolation, ToContentJump, Shift
        public int InputCount => _heads * (_m + 2 + GetShiftInputs());

        public int OutputCount => _m * _heads;

        public double[][] TapeValues => Utilities.DeepCopy(_tape.ToArray());

        // PRIVATE HELPER METHODS

        private static double[] Take(double[] values, int start, int amount)
        {
            double[] result = new double[amount];
            for (int i = 0; i < amount; i++)
                result[i] = values[i + start];
            return result;
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append(Utilities.ToString(_tape));
            b.Append("\n");
            b.Append("Pointers=");
            b.Append(Utilities.ToString(_headPositions));
            return b.ToString();
        }

        private int GetShiftInputs()
        {
            switch (_shiftMode)
            {
                case ShiftMode.Single: return 1;
                default: return _shiftLength;
            }
        }

        private string PrintState()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("TM Tape: \n");
            for (int i = 0; i < _tape.Count; i++)
            {
                builder.Append("| ");
                builder.Append(Utilities.ToString(_tape[i]));
                builder.Append("|");
                builder.Append($" reads: {_readActivities[i]}, writes: {_writeActivities[i]}");
                builder.Append("\n");
            }
            builder.Append("\nHeadPositions: ");
            builder.Append(Utilities.ToString(_headPositions, "n0"));
            return builder.ToString();
        }

        private void Write(int head, double[] content, double interp)
        {
            _tape[_headPositions[head]] = Interpolate(content, _tape[_headPositions[head]], interp);

            //Log write activities
            _writeActivities[_headPositions[head]]++;
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
                for (int i = 0; i < _tape.Count; i++)
                {
                    double curSim = Utilities.Emilarity(key, _tape[i]);

                    //Debug.Log("Pos " + i + ": sim =" + curSim + (curSim > similarity ? " better" : ""), true);

                    if (curSim > similarity)
                    {
                        similarity = curSim;
                        bestPos = i;
                    }
                }

                Debug.Log($"Content Jump Head {head} from {_headPositions[head]} to {bestPos}", true);

                _headPositions[head] = bestPos;

            }
            else
            {
                Debug.Log("No content jump", true);
            }
        }

        private void MoveHead(int head, double[] shift)
        {
            // SHIFTING
            int highest;
            switch (_shiftMode)
            {
                case ShiftMode.Single: highest = (int)(shift[0] * _shiftLength); break; // single
                default: highest = Utilities.MaxPos(shift); break; // multiple
            }

            int offset = highest - (_shiftLength / 2);

            //Debug.Log("Highest=" + highest, true);
            //Debug.Log("Offset=" + offset, true);

            while (offset != 0)
            {
                if (offset > 0)
                {
                    if (_n > 0 && _tape.Count >= _n)
                    {
                        _headPositions[head] = 0;
                    }
                    else
                    {
                        _headPositions[head] = _headPositions[head] + 1;

                        if (_headPositions[head] >= _tape.Count)
                        {
                            _tape.Add(new double[_m]);

                            // extend debug logs for read and write activities
                            _writeActivities.Add(0);
                            _readActivities.Add(0);
                        }
                    }

                }
                else
                {
                    if (_n > 0 && _tape.Count >= _n)
                    {
                        _headPositions[head] = _tape.Count - 1;
                    }
                    else
                    {
                        _headPositions[head] = _headPositions[head] - 1;
                        // if we shift below index 0, we have to add elements to the start of the tape and move all heads accordingly
                        if (_headPositions[head] < 0)
                        {
                            _tape.Insert(0, new double[_m]);

                            // extend debug logs for read and write activities
                            _writeActivities.Insert(0, 0);
                            _readActivities.Insert(0, 0);

                            // Moving all heads accordingly
                            for (int i = 0; i < _heads; i++)
                            {
                               _headPositions[i] = _headPositions[i] + 1;
                            }

                            _increasedSizeDown = true;
                        }
                    }
                }

                Debug.Log($"Shift Head {head} by {offset} to {_headPositions[head]}", true);
                offset = offset > 0 ? offset - 1 : offset + 1; // Go closer to 0
            }
        }

        private double[] GetRead(int head)
        {
            // log debug read activity
            _readActivities[_headPositions[head]]++;

            return (double[])_tape[_headPositions[head]].Clone();
        }
    }
}
