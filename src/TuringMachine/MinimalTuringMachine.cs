using System;
using System.Collections.Generic;
using System.Text;
using ENTM.NoveltySearch;
using ENTM.Utility;

namespace ENTM.TuringMachine
{
    /// <summary>
    /// Turing machine modified for Novelty Search and other optimizations.
    /// Builds on original java version by Emil Juul Jacobsen and Rasmus Boll Greve.
    /// 
    /// Created by Benno Lüders and Mikkel Schläger.
    /// 
    /// 
    /// Original doc:
    /// 
    /// * Our simplified version of a Turing Machine for
    /// * use with a neural network.It uses the general
    /// * TuringMachine interface so can be used in the
    /// * same contexts as the GravesTuringMachine.
    /// * 
    /// * @author Emil
    /// </summary>
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

        // similarity threshold to check if a write changed the content of the tape at one location
        private double _didWriteThreshold;

        // The minimal similarity for the tm to jump to that position, if non is found that satisfies this, the tm will
        // jump to the start of the tape.
        private double _minSimilarityToJump;

        // (Fixed length tape) Do we initialize the tape with a gradient from 0-1?
        private bool _initalizeWithGradient;

        // If we do not use a gradient to initialize, what is the initial value of the tape locations?
        private double _initalValue;

        // If we do not use a gradient to initialize, what is the initial value of the tape locations?
        private bool _useMemoryExpandLocation;

        public NoveltySearchInfo NoveltySearch { get; set; }

        public int ReadHeadCount => 1;

        public int WriteHeadCount => 1;

        // WriteKey, Interpolation, ToContentJump, Shift
        public int InputCount => _heads * (_m + 2 + GetShiftInputs());

        public int OutputCount => _m * _heads;

        public double[][] TapeValues => Utilities.DeepCopy(_tape.ToArray());

        public double[][] DefaultRead => _initialRead;

        private int EndOfTape => _tape.Count - 1;

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
            _initalizeWithGradient = props.InitalizeWithGradient;
            _initalValue = props.InitalValue;
            _didWriteThreshold = props.DidWriteThreshold;
            _useMemoryExpandLocation = props.UseMemoryExpandLocation;

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

            if (_initalizeWithGradient)
            {
                InitTapeWithGradient();
            }

            if (_initalValue != 0)
            {
                if (_initalizeWithGradient)
                {
                    throw new ArgumentOutOfRangeException("Tape can not be initialized with a fixed value and gradient. Choose one.");
                }
                InitTapeWithFixedValue();
            }

            _headPositions = new int[_heads];

            // clear debug log lists
            //_readActivities.Clear();
            //_readActivities.Add(0);
            //_writeActivities.Clear();
            //_writeActivities.Add(0);

            _zeroPosition = 0;
            _increasedSizeDown = false;

            Debug.DLog(PrintState(), true);
        }

        private void InitTapeWithGradient()
        {
            // add missing locations
            for (int i = 1; i < _n; i++)
            {
                _tape.Add(new double[_m]);
            }

            // make gradient
            for (int i = 0; i < _n; i++)
            {
                for (int j = 0; j < _m; j++)
                {
                    _tape[i][j] = ((double)i) / _tape.Count;
                }
            }
        }

        private void InitTapeWithFixedValue()
        {
            // add missing locations
            for (int i = 1; i < _n; i++)
            {
                _tape.Add(new double[_m]);
            }

            // fill with values
            for (int i = 0; i < _n; i++)
            {
                for (int j = 0; j < _m; j++)
                {
                    _tape[i][j] = _initalValue;
                }
            }
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
            if (RecordTimeSteps || NoveltySearch.ScoreNovelty)
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

                if (RecordTimeSteps || NoveltySearch.ScoreNovelty)
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
                int shift = MoveHead(i, shifts[i]);

                // 4: Read!
                result[i] = GetRead(i); // Show me what you've got! \cite{rickEtAl2014}

                if (RecordTimeSteps || NoveltySearch.ScoreNovelty)
                {
                    // Calculate corrected write position first, since the write happened before a possible downward memory increase
                    int correctedWritePosition = writePositions[i] - _zeroPosition;

                    // The memory has increased in size downward (-1), shifting all memory positions +1
                    if (_increasedSizeDown)
                    {
                        writePositions[i]++;
                        _zeroPosition++;
                    }

                    if (NoveltySearch.ScoreNovelty)
                    {
                        // (-1 because timestep 0 is not scored)
                        int n = _currentTimeStep - 1;
                        switch (NoveltySearch.VectorMode)
                        {
                            case NoveltyVectorMode.WritePattern:

                                // Save the write position as a behavioural trait 
                                NoveltySearch.NoveltyVectors[n][0] = correctedWritePosition;

                                break;

                            case NoveltyVectorMode.ReadContent:

                                // Content of the read vector
                                Array.Copy(result[i], NoveltySearch.NoveltyVectors[n], result[i].Length);

                                break;

                            case NoveltyVectorMode.WritePatternAndInterp:

                                // Head position (we use corrected write position to account for downward shifts)
                                NoveltySearch.NoveltyVectors[n][0] = correctedWritePosition;

                                // Write interpolation
                                NoveltySearch.NoveltyVectors[n][1] = interps[i];

                                break;

                            case NoveltyVectorMode.ShiftJumpInterp:

                                NoveltySearch.NoveltyVectors[n][0] = shift;
                                NoveltySearch.NoveltyVectors[n][1] = jumps[i];
                                NoveltySearch.NoveltyVectors[n][2] = interps[i];

                                break;

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

                        if (NoveltySearch.VectorMode != NoveltyVectorMode.EnvironmentAction)
                        {
                            // If the turing machine neither wrote or read this iteration, we note it for the minimum criteria novelty search
                            if (!_didWrite && !_didRead)
                            {
                                // Store number of redundant iterations in the novelty vector.
                                NoveltySearch.MinimumCriteria[0] += 1;
                            }

                            // Total timesteps to calculate redundancy factor
                            NoveltySearch.MinimumCriteria[1] += 1;
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
                    throw new ArgumentException("Unrecognized Shift Mode: " + _shiftMode);
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
            double[] preWrite = null;

            // Store the tape data before the write
            preWrite = _tape[_headPositions[head]];

            switch (_writeMode)
            {
                case WriteMode.Interpolate:
                    _tape[_headPositions[head]] = Interpolate(content, _tape[_headPositions[head]], interp);
                    break;

                case WriteMode.Overwrite:
                    // Overwrite mode will overwrite existing data completely
                    if (interp >= .5f)
                    {
                        _tape[_headPositions[head]] = content;
                    }
                    break;
            }

            // Compare the tape position before and after write
            double similarity = Utilities.Emilarity(preWrite, _tape[_headPositions[head]]);
            if (similarity <= _didWriteThreshold)
            {
                // If the vectors are not similar, it means we wrote to the tape.
                _didWrite = true;

                // the end of the tape will be our expand location, if we write here, we need to create a new expand location for new memories
                if (_useMemoryExpandLocation && _headPositions[head] == EndOfTape)
                {

                    // Expand the memory to the right (end of array)
                    ExpandTapeRight();
                }
            }

            //Log write activities
            //_writeActivities[_headPositions[head]]++;
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
            if (contentJump >= .5)
            {
                // JUMPING POINTER TO BEST MATCH
                int best = 0;
                double similarity = double.MinValue;
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

                    // Perfect similarity, we don't need to check the rest
                    if (similarity > 0.9999) break;
                }

                Debug.DLog($"Content Jump Head {head} from {_headPositions[head]} to {best}", true);

                // Jump the head to the best position found, or to the end if no simillar position was found
                // Check if similarity meets lower threshold
                if (similarity >= _minSimilarityToJump)
                {
                    _headPositions[head] = best;
                }
                else
                {
                    _headPositions[head] = EndOfTape;
                }
            }
            else
            {
                Debug.DLog("No content jump", true);
            }
        }

        private int MoveHead(int head, double[] shift)
        {
            // SHIFTING
            int highest;
            switch (_shiftMode)
            {
                case ShiftMode.Single:
                    highest = (int)(shift[0] * _shiftLength);
                    break;

                case ShiftMode.Multiple:
                    highest = Utilities.MaxPos(shift);
                    break;

                default:
                    throw new ArgumentException("Unrecognized Shift Mode: " + _shiftMode);
            }

            int offset = highest - (_shiftLength / 2);

            int result = offset;

            //Debug.Log("Highest=" + highest, true);
            //Debug.Log("Offset=" + offset, true);

            while (offset != 0)
            {
                // Right shift (positive)
                if (offset > 0)
                {
                    // Wrap around if memory size is limited and the limit is reached
                    if (_n > 0 && _tape.Count >= _n && _headPositions[head] == EndOfTape)
                    {
                        _headPositions[head] = 0;
                    }
                    else
                    {
                        _headPositions[head] += 1;

                        // Expand the memory to the right (end of array)
                        if (_headPositions[head] >= _tape.Count)
                        {
                            ExpandTapeRight();

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
                        _headPositions[head] = EndOfTape;
                    }
                    else
                    {
                        _headPositions[head] -= 1;

                        // if we shift below index 0, we have to add elements to the start of the tape and move all heads accordingly
                        if (_headPositions[head] < 0)
                        {
                            ExpandTapeLeft();
                        }
                    }
                }

                Debug.DLog($"Shift Head {head} by {offset} to {_headPositions[head]}", true);

                offset = offset > 0 ? offset - 1 : offset + 1; // Go closer to 0
            }

            return result;
        }

        private double[] GetRead(int head)
        {
            // log debug read activity
            //_readActivities[_headPositions[head]]++;

            return (double[])_tape[_headPositions[head]].Clone();
        }

        private double[] ExpandTapeLeft()
        {
            double[] newLocation = new double[_m];
            if (_initalValue != 0)
            {
                for (int i = 0; i < _m; i++)
                {
                    newLocation[i] = _initalValue;
                }
            }
            _tape.Insert(0, newLocation);

            // extend debug logs for read and write activities
            // _writeActivities.Insert(0, 0);
            // _readActivities.Insert(0, 0);

            // Moving all heads accordingly
            for (int i = 0; i < _heads; i++)
            {
                _headPositions[i] = _headPositions[i] + 1;
            }

            _increasedSizeDown = true;
            return newLocation;
        }

        private double[] ExpandTapeRight()
        {
            double[] newLocation = new double[_m];
            if (_initalValue != 0) {
                for (int i = 0; i < _m; i++)
                {
                    newLocation[i] = _initalValue;
                }
            }

            _tape.Add(newLocation);

            return newLocation;
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
