
// #define DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using ENTM.NoveltySearch;
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

        // The number of memory locations in the FINITE tape. If this is -1, the tape is infinite (theoretically)
        private readonly int _n;

        // The maximum distance you can jump with shifting.
        private readonly int _shiftLength;

        // Single or multiple shifts
        private readonly ShiftMode _shiftMode;

        // Interpolate or absolute write mode
        private readonly WriteMode _writeMode;

        // If false, the turing machine always returns initial read
        private readonly bool _enabled;

        // Number of combined read/write heads
        private readonly int _heads;

        private int _currentTimeStep;

        private double[][] _initialRead;

        private bool _increasedSizeDown;

        private int _zeroPosition;
        private bool _didWrite, _didRead;

        // The minimal similarity for the tm to jump to that position, if non is found that satisfies this, the tm will
        // jump to the start of the tape.
        private double _minSimilarityToJump;

        public bool ScoreNovelty { get; set; }

        public int NoveltyVectorLength { get; set; }

        public NoveltyVector NoveltyVectorMode { get; set; }

        private double[] _noveltyVector;

        public double[] NoveltyVector => _noveltyVector;

        public int ReadHeadCount => 1;

        public int WriteHeadCount => 1;
        
        // WriteKey, Interpolation, ToContentJump, Shift
        public int InputCount => _heads * (_m + 2 + GetShiftInputs());

        public int OutputCount => _m * _heads;

        public double[][] TapeValues => Utilities.DeepCopy(_tape.ToArray());

        public double[][] DefaultRead => _initialRead;

        // Number of times each location was accessed during livetime of the tm
        // private List<int> _writeActivities = new List<int>();
        // private List<int> _readActivities = new List<int>();

        public MinimalTuringMachine(TuringMachineProperties props)
        {
            _enabled = props.Enabled;
            _m = props.M;
            _n = props.N;
            _heads = props.Heads;
            _shiftLength = props.ShiftLength;
            _shiftMode = props.ShiftMode;
            _writeMode = props.WriteMode;
            _minSimilarityToJump = props.MinSimilarityToJump;
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
            Debug.DLogHeader("TURING MACHINE RESET", true);

            _currentTimeStep = 0;

            _tape.Clear();
            _tape.Add(new double[_m]);
            _headPositions = new int[_heads];

            if (ScoreNovelty)
            {
                _noveltyVector = new double[NoveltyVectorLength];
                _noveltyVector[0] = 0;
            }

            // clear debug log lists
            //_readActivities.Clear();
            //_readActivities.Add(0);
            //_writeActivities.Clear();
            //_writeActivities.Add(0);

            _zeroPosition = 0;
            _increasedSizeDown = false;

            Debug.DLog(PrintState(), true);
        }

        /// <summary>
        /// Activate the Turing machine.
        /// Operation order:
        ///	1: Write
        /// 2: Content jump
        /// 3: Shift
        /// 4: Read
        /// </summary>
        /// <param name="fromNN">Turing machine input (NN output)</param>
        /// <returns>Turing machine output (NN input)</returns>
        public double[][] ProcessInput(double[] fromNN)
        {
            Debug.DLogHeader("MINIMAL TURING MACHINE START", true);
            Debug.DLog($"From NN: {Utilities.ToString(fromNN, "f4")}", true);

            // Current timestep will start on 1, since the inital read does not run this method
            _currentTimeStep++;

            if (!_enabled) return _initialRead;

            double[][] result = new double[_heads][];

            double[][] writeKeys = new double[_heads][];
            double[] interps = new double[_heads];
            double[] jumps = new double[_heads];
            double[][] shifts = new double[_heads][];

            int[] writePositions = null;
            double[][] written = null;
            _didWrite = false;
            _didRead = false;

            // Attention! Novelty score does not currently support multiple read/write heads.
            // It will overwrite data for previous heads, if more than one.
            if (RecordTimeSteps || ScoreNovelty)
            {
                writePositions = new int[_heads];

                if (RecordTimeSteps)
                {
                    written = new double[_heads][];
                }
            }
            
            int p = 0;

            // First all writes
            for (int i = 0; i < _heads; i++)
            {
                writeKeys[i] = Take(fromNN, p, _m);
                p += _m;

                interps[i] = fromNN[p];
                p++;

                jumps[i] = fromNN[p];
                p++;

                int s = GetShiftInputs();
                shifts[i] = Take(fromNN, p, s);
                p += s;

                Debug.DLogHeader($"HEAD {i + 1}", true);
                Debug.DLog($"\nWrite:        \t{Utilities.ToString(writeKeys[i], "f4")}" +
                          $"\nInterpolate:  \t{interps[i].ToString("f4")}" +
                          $"\nContent:      \t{jumps[i].ToString("f4")}" +
                          $"\nShift:        \t{Utilities.ToString(shifts[i], "f4")}", true);

                // 1: Write!
                Write(i, writeKeys[i], interps[i]);

                if (RecordTimeSteps || ScoreNovelty)
                {
                    // Save write position for recording
                    writePositions[i] = _headPositions[i];

                    if (RecordTimeSteps)
                    {
                        //Save tape data at write location before head is moved
                        written[i] = GetRead(i);
                    }
                }
            }

            for (int i = 0; i < _heads; i++)
            {
                // 2: Content jump!
                PerformContentJump(i, jumps[i], writeKeys[i]);
            }

            // Shift and read (no interaction)
            for (int i = 0; i < _heads; i++)
            {
               
                // If the memory has been expanded down below 0, which means the memory has shiftet
                _increasedSizeDown = false;

                // 3: Shift!
                MoveHead(i, shifts[i]);

                // 4: Read!
                result[i] = GetRead(i); // Show me what you've got! \cite{rickEtAl2014}

                if (RecordTimeSteps || ScoreNovelty)
                {
                    // Calculate corrected write position first, since the write happened before a possible downward memory increase
                    int correctedWritePosition = writePositions[i] - _zeroPosition;

                    // The memory has increased in size downward (-1), shifting all memory positions +1
                    if (_increasedSizeDown)
                    {
                        writePositions[i]++;
                        _zeroPosition++;
                    }

                    if (ScoreNovelty)
                    {
                        switch (NoveltyVectorMode)
                        {
                            case NoveltySearch.NoveltyVector.WritePattern:
                                // Save the write position as a behavioural trait
                                _noveltyVector[_currentTimeStep] = correctedWritePosition;

                                break;

                            case NoveltySearch.NoveltyVector.ReadContent:

                                // Skip position 1, it's used for minimum criteria. Timestep 0 is not recorded here, so subtract 1
                                int startIndex = 1 + (_currentTimeStep - 1) * _m;
                                Array.Copy(result[i], 0, _noveltyVector, startIndex, result[i].Length);

                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                   

                        // Check for non-empty reads, to see if novelty search minimum criteria has been reached
                        for (int j = 0; j < result[i].Length; j++)
                        {
                            if (result[i][j] > .1f)
                            {
                                _didRead = true;
                                break;
                            }
                        }

                        // If the turing machine neither wrote or read this iteration, we note it for the minimum criteria novelty search
                        if (!_didWrite && !_didRead)
                        {
                            // Store number of redundant iterations in the novelty vector.
                            _noveltyVector[0] += 1;
                        }
                    }

                    if (RecordTimeSteps)
                    {
                        int readPosition = _headPositions[i];
                        int correctedReadPosition = readPosition - _zeroPosition;

                        _prevTimeStep = new TuringMachineTimeStep(writeKeys[i], interps[i], jumps[i], shifts[i], result[i], written[i], writePositions[i], readPosition, _zeroPosition, correctedWritePosition, correctedReadPosition, _tape.Count);
                    }
                }
            }

            Debug.DLog(PrintState(), true);
            Debug.DLog("Sending to NN: " + Utilities.ToString(result, "F4"), true);
            Debug.DLogHeader("MINIMAL TURING MACHINE END", true);

            return result;
        }


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
                case ShiftMode.Single:
                    return 1;
                case ShiftMode.Multiple:
                    return _shiftLength;
                default:
                    throw new ArgumentException("Unrecognized Shift Mode: " + _shiftMode.ToString());
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
                //builder.Append($" reads: {_readActivities[i]}, writes: {_writeActivities[i]}");
                builder.Append("\n");
            }
            builder.Append("\nHeadPositions: ");
            builder.Append(Utilities.ToString(_headPositions, "n0"));
            return builder.ToString();
        }

        private void Write(int head, double[] content, double interp)
        {
            switch (_writeMode)
            {
                case WriteMode.Interpolate:
                    _tape[_headPositions[head]] = Interpolate(content, _tape[_headPositions[head]], interp);

                    // We set a minimum threshold for write interpolation required to reach the novelty search minimum criteria
                    if (interp > .1f)
                    {
                        _didWrite = true;
                    }
                    break;

                case WriteMode.Absolute:
                    // Absolute mode will overwrite completely
                    if (interp >= .5f)
                    {
                        _tape[_headPositions[head]] = content;
                        _didWrite = true;
                    }
                    break;
            }

            //Log write activities
            //_writeActivities[_headPositions[head]]++;
        }

        private double[] Interpolate(double[] first, double[] second, double interp)
        {
            double[] result = new double[first.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = interp*first[i] + (1 - interp)*second[i];
            }
            return result;
        }

        private void PerformContentJump(int head, double contentJump, double[] key)
        {
            if (contentJump >= .5)
            {
                // JUMPING POINTER TO BEST MATCH
                int best = 0;
                double similarity = double.MinValue;
                bool doJump = false;
                for (int i = 0; i < _tape.Count; i++)
                {
                    double curSim = Utilities.Emilarity(key, _tape[i]);

                    //Debug.Log("Pos " + i + ": sim =" + curSim + (curSim > similarity ? " better" : ""), true);

                    // We deterministically select the first max value, if more than one is found
                    if (curSim > similarity)
                    {
                        similarity = curSim;
                        best = i;
                    }

                    if (_minSimilarityToJump > similarity)
                    {
                        continue;
                    }

                    doJump = true;
                    // Perfect similarity, we don't need to check the rest
                    if (similarity > 0.9999) break;
                }

                Debug.DLog($"Content Jump Head {head} from {_headPositions[head]} to {best}", true);

                // Jump the head to the best position found, or to the end if no simillar position was found
                if (doJump)
                {
                    _headPositions[head] = best;
                }
                else
                {
                    _headPositions[head] = _tape.Count -1;
                }
            }
            else
            {
                Debug.DLog("No content jump", true);
            }
        }

        private void MoveHead(int head, double[] shift)
        {
            // SHIFTING
            int highest;
            switch (_shiftMode)
            {
                case ShiftMode.Single:
                    highest = (int) (shift[0]*_shiftLength);
                    break;
                case ShiftMode.Multiple:
                    highest = Utilities.MaxPos(shift);
                    break;
                default:
                    throw new ArgumentException("Unrecognized Shift Mode: " + _shiftMode.ToString());
            }

            int offset = highest - (_shiftLength/2);

            //Debug.Log("Highest=" + highest, true);
            //Debug.Log("Offset=" + offset, true);

            while (offset != 0)
            {
                // Right shift (positive)
                if (offset > 0)
                {
                    // Wrap around if memory size is limited and the limit is reached
                    if (_n > 0 && _tape.Count >= _n && _headPositions[head] == _tape.Count - 1)
                    {
                        _headPositions[head] = 0;
                    }
                    else
                    {
                        _headPositions[head] += 1;

                        // Expand the memory to the right (end of array)
                        if (_headPositions[head] >= _tape.Count)
                        {
                            _tape.Add(new double[_m]);

                            // extend debug logs for read and write activities
                            //_writeActivities.Add(0);
                            //_readActivities.Add(0);
                        }
                    }
                }
                // Left shift (negative)
                else
                {
                    // Wrap around if memory size is limited and the limit is reached
                    if (_n > 0 && _tape.Count >= _n && _headPositions[head] == 0)
                    {
                        _headPositions[head] = _tape.Count - 1;
                    }
                    else
                    {
                        _headPositions[head] -= 1;

                        // if we shift below index 0, we have to add elements to the start of the tape and move all heads accordingly
                        if (_headPositions[head] < 0)
                        {
                            _tape.Insert(0, new double[_m]);

                            // extend debug logs for read and write activities
                            // _writeActivities.Insert(0, 0);
                            // _readActivities.Insert(0, 0);

                            // Moving all heads accordingly
                            for (int i = 0; i < _heads; i++)
                            {
                                _headPositions[i] = _headPositions[i] + 1;
                            }

                            _increasedSizeDown = true;
                        }
                    }
                }

                Debug.DLog($"Shift Head {head} by {offset} to {_headPositions[head]}", true);

                offset = offset > 0 ? offset - 1 : offset + 1; // Go closer to 0
            }
        }

        private double[] GetRead(int head)
        {
            // log debug read activity
            //_readActivities[_headPositions[head]]++;

            return (double[]) _tape[_headPositions[head]].Clone();
        }

        #region Replayable

        private TuringMachineTimeStep _prevTimeStep;

        public bool RecordTimeSteps { get; set; } = false;

        public TuringMachineTimeStep InitialTimeStep
        {
            get { return _prevTimeStep = new TuringMachineTimeStep(new double[_m], 0, 0, new double[_shiftLength], new double[_m], new double[_m], 0, 0, 0, 0, 0, 1); }
        }

        public TuringMachineTimeStep PreviousTimeStep => _prevTimeStep;

        #endregion
    }
}
