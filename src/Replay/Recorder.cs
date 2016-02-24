using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.TuringMachine;
using ENTM.Utility;

namespace ENTM.Replay
{
    public class Recorder
    {
        private List<TimeStep> _recordedTimeSteps;

        public void Start()
        {
            _recordedTimeSteps = new List<TimeStep>();
        }

        public void Record(EnvironmentTimeStep environmentTimeStep, TuringMachineTimeStep turingTimeStep)
        {
            _recordedTimeSteps.Add(new TimeStep(environmentTimeStep, turingTimeStep));
        }

        public Bitmap ToBitmap()
        {
            int w = _recordedTimeSteps.Count;

            TimeStep finalStep = _recordedTimeSteps[_recordedTimeSteps.Count - 1];
            int memSize = finalStep.TuringMachineTimeStep.MemorySize;
            int zero = finalStep.TuringMachineTimeStep.ZeroPosition;

            int[] startIndex = new int[11];
            startIndex[0] = 0;
            startIndex[1] = startIndex[0] + finalStep.EnvironmentTimeStep.Output.Length + 1;
            startIndex[2] = startIndex[1] + finalStep.EnvironmentTimeStep.Input.Length + 1;
            startIndex[3] = startIndex[2] + finalStep.EnvironmentTimeStep.Input.Length + 1;
            startIndex[4] = startIndex[3] + 2; // Score
            startIndex[5] = startIndex[4] + finalStep.TuringMachineTimeStep.Key.Length + 1;
            startIndex[6] = startIndex[5] + 2; // Interp
            startIndex[7] = startIndex[6] + 2; // Jump
            startIndex[8] = startIndex[7] + finalStep.TuringMachineTimeStep.Shift.Length + 1;
            startIndex[9] = startIndex[8] + finalStep.TuringMachineTimeStep.Read.Length + 1;
            startIndex[10] = startIndex[9] + memSize + 1;

            int h = startIndex[10] + memSize;

            Bitmap bmp = new Bitmap(w, h);
            for (int x = 0; x < w; x++)
            {
                int offset = 0;
                TimeStep t = _recordedTimeSteps[x];
                for (int y = 0; y < h; y++)
                {
                    Color pixel = default(Color);

                    foreach (int index in startIndex)
                    {
                        if (y == index - 1)
                        {
                            offset = index;
                            pixel = Color.Gray;
                            goto EndOfInner;
                        }
                    }

                    int i = y - offset;
                    if (y < startIndex[1])
                    {
                        pixel = ColorUtils.BlackAndWhite(t.EnvironmentTimeStep.Output[i]);
                    }
                    else if (y < startIndex[2])
                    {
                        pixel = ColorUtils.BlackAndWhite(t.EnvironmentTimeStep.Input[i]);
                    }
                    else if (y < startIndex[3])
                    {
                        if (x > _recordedTimeSteps.Count / 2 + 1)
                        {
                            TimeStep input = _recordedTimeSteps[x - (_recordedTimeSteps.Count/2)];
                            double v = Math.Abs(input.EnvironmentTimeStep.Output[i + 2] - t.EnvironmentTimeStep.Input[i]);
                            pixel = ColorUtils.BlackAndWhite(v);
                        }
                        else
                        {
                            pixel = Color.Black;
                        }
                    }
                    else if (y < startIndex[4])
                    {
                        pixel = doubleToPixelColorScale(t.EnvironmentTimeStep.Score);
                    }
                    else if (y < startIndex[5])
                    {
                        pixel = doubleToPixelColorScale(t.TuringMachineTimeStep.Key[i]);
                    }
                    else if (y < startIndex[6])
                    {
                        pixel = doubleToPixelColorScale(t.TuringMachineTimeStep.WriteInterpolation);
                    }
                    else if (y < startIndex[7])
                    {
                        pixel = doubleToPixelColorScale(t.TuringMachineTimeStep.ContentJump);
                    }
                    else if (y < startIndex[8])
                    {
                        pixel = doubleToPixelColorScale(t.TuringMachineTimeStep.Shift[i]);
                    }
                    else if (y < startIndex[9])
                    {
                        pixel = doubleToPixelColorScale(t.TuringMachineTimeStep.Read[i]);
                    }
                    else if (y < startIndex[10])
                    {
                        pixel = (i - zero) == t.TuringMachineTimeStep.CorrectedWritePosition ? Color.White : Color.Black;
                    }
                    else
                    {
                        pixel = (i - zero) == t.TuringMachineTimeStep.CorrectedReadPosition ? Color.White : Color.Black;
                    }

                    EndOfInner:

                    bmp.SetPixel(x, y, pixel);

                }
            }

            return bmp;
        }

        private Color doubleToPixelColorScale(double v)
        {
            return ColorUtils.ColorFromHSV((1d - v) * 240, 1, 1);
        }

        internal struct TimeStep
        {
            public EnvironmentTimeStep EnvironmentTimeStep { get; }
            public TuringMachineTimeStep TuringMachineTimeStep { get; }

            public TimeStep(EnvironmentTimeStep environmentTimeStep, TuringMachineTimeStep turingMachineTimeStep)
            {
                EnvironmentTimeStep = environmentTimeStep;
                TuringMachineTimeStep = turingMachineTimeStep;
            }

        }
    }
}
