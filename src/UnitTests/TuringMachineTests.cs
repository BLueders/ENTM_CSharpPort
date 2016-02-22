using Microsoft.VisualStudio.TestTools.UnitTesting;
using ENTM.TuringMachine;

namespace UnitTests
{
    [TestClass]
    public class TuringMachineTests
    {
        MinimalTuringMachine _tm;

        [TestInitialize()]
        public void Before()
        {
            TuringMachineProperties props = new TuringMachineProperties
            {
                M = 2,
                N = -1,
                ShiftLength = 3,
                ShiftMode = ShiftMode.Multiple,
                Enabled = true,
                Heads = 1
            };
            _tm = new MinimalTuringMachine(props);
        }

        [TestMethod]
        public void TestShift()
        {
            double[] tmstim = {
                0, 1, //Write
                1, //Write interpolation
                0, //Content jump
                1, 0, 0 //Shift
            };

            double[] result = _tm.ProcessInput(tmstim)[0];
            double[] expected = {0, 0};
            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(expected[i], result[i], 0.0001);
            }

            tmstim = new double[]
            {
                0, 0, //Write
                0, //Write interpolation
                0, //Content jump
                0, 0, 1 //Shift
            };

            result = _tm.ProcessInput(tmstim)[0];
            expected = new double[] {0, 1};
            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(expected[i], result[i], 0.0001);
            }
        }

        [TestMethod]
        public void TestContentBasedJump()
        {
            //Write jump target
            double[] tmstim = {
                0, 1, //Write
                1, //Write interpolation
                0, //Content jump
                0, 0, 1 //Shift
            };
            double[] result = _tm.ProcessInput(tmstim)[0];

            //Write jump result
            tmstim = new double[]
            {
                1, 0, //Write
                1, //Write interpolation
                0, //Content jump
                0, 0, 1 //Shift
            };
            result = _tm.ProcessInput(tmstim)[0];

            //Jump, shift, and read
            tmstim = new double[]
            {
                0, 1, //Write
                0, //Write interpolation
                1, //Content jump
                0, 0, 1 //Shift
            };
            result = _tm.ProcessInput(tmstim)[0];
            double[] expected = {1, 0};
            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(expected[i], result[i], 0.0001);
            }
        }

        [TestMethod]
        public void TestContentBasedJumpLonger()
        {
            //Write jump target
            double[] tmstim = {
                0, 1, //Write
                1, //Write interpolation
                0, //Content jump
                0, 0, 1 //Shift
            };
            double[] result = _tm.ProcessInput(tmstim)[0];

            //Write jump result
            tmstim = new double[]
            {
                1, 0, //Write
                1, //Write interpolation
                0, //Content jump
                0, 0, 1 //Shift
            };
            result = _tm.ProcessInput(tmstim)[0];

            //Move right
            for (int k = 0; k < 10; k++)
            {
                tmstim = new double[]
                {
                    0, 0, //Write
                    0, //Write interpolation
                    0, //Content jump
                    0, 0, 1 //Shift
                };
                result = _tm.ProcessInput(tmstim)[0];
            }
            //Jump, shift, and read
            tmstim = new double[]
            {
                0, 1, //Write
                0, //Write interpolation
                1, //Content jump
                0, 0, 1 //Shift
            };
            result = _tm.ProcessInput(tmstim)[0];
            double[] expected = {1, 0};
            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(expected[i], result[i], 0.0001);
            }
        }

        [TestMethod]
        public void TestCopyTaskSimple()
        {
            double[][] seq = {
                new double[] {0, 1, 0}, //Start
                new double[] {1, 0, 0}, //Data
                new double[] {0, 0, 0}, //Data
                new double[] {0, 0, 0}, //Data
                new double[] {1, 0, 0}, //Data
                new double[] {1, 0, 0}, //Data
                new double[] {0, 0, 1}, //End
                new double[] {0, 0, 0}, //Poll
                new double[] {0, 0, 0}, //Poll
                new double[] {0, 0, 0}, //Poll
                new double[] {0, 0, 0}, //Poll
                new double[] {0, 0, 0}, //Poll
            };

            double[] lastRoundRead = {0, 0};

            for (int round = 0; round < seq.Length; round++)
            {

                double d = seq[round][0];
                double s = seq[round][1];
                double b = seq[round][2];

                lastRoundRead = Act(d + lastRoundRead[0], s + b + lastRoundRead[1], 1 - b, b, 0, b, 1 - b);
                double roundResult = lastRoundRead[0];

                if (round > 6)
                {
                    double verify = seq[round - 6][0];
                    Assert.AreEqual(verify, roundResult, 0.000001);
                }
            }
        }

        private double[] Act(double d1, double d2, double write, double jump, double shiftLeft, double shiftStay,
            double shiftRight)
        {
            return _tm.ProcessInput(new[]
            {
                d1, d2, //Write
                write, //Write interpolation
                jump, //Content jump
                shiftLeft, shiftStay, shiftRight //Shift
            })[0];
        }
    }
}
