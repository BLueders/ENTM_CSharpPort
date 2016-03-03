using System;
using System.Text;
using System.Collections.Generic;
using System.Configuration;
using ENTM.Experiments.CopyTask;
using ENTM.TuringMachine;
using ENTM.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Domains;

namespace UnitTests
{
    /// <summary>
    /// Summary description for CopyTaskEnvironmentTests
    /// </summary>
    [TestClass]
    public class CopyTaskEnvironmentTests
    {
        private CopyTaskEnvironment _cpTaskEnv;
        private readonly double[] _dummyAction = { 0, 0 };

        [TestInitialize()]
        public void Before()
        {
            _cpTaskEnv = CreateEnvironment(FitnessFunction.StrictCloseToTarget);
            _cpTaskEnv.ResetAll();
        }

        private CopyTaskEnvironment CreateEnvironment(FitnessFunction fitnessFunction)
        {
            CopyTaskProperties props = new CopyTaskProperties
            {
                Iterations = 10,
                VectorSize = 2,
                FitnessFunction = fitnessFunction,
                MaxSequenceLength = 10,
                LengthRule = LengthRule.Fixed,
            };
            return new CopyTaskEnvironment(props);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        private void FastForwardToStep(CopyTaskEnvironment env, int step)
        {
            double[] initialObservation = _cpTaskEnv.InitialObservation;
            double[] dummyAction = new double[] { 0, 0 };
            for (int i = 1; i < step; i++)
            {
                _cpTaskEnv.PerformAction(dummyAction);
            }
        }

        [TestMethod]
        public void TestStartBit()
        {
            CollectionAssert.AreEqual(new double[] {1,0,0,0}, _cpTaskEnv.InitialObservation, "Starting bit");

        }

        [TestMethod]
        public void TestWritePhase()
        {
            FastForwardToStep(_cpTaskEnv, 1);
            for (int i = 0; i < _cpTaskEnv.Sequence.Length; i++)
            {
                double[] result = Utilities.JoinArrays(new double[] { 0, 0 }, _cpTaskEnv.Sequence[i]);
                CollectionAssert.AreEqual(result, _cpTaskEnv.PerformAction(_dummyAction), $"Sequence write {i}");
            }
        }

        [TestMethod]
        public void TestDelimiterBit()
        {
            FastForwardToStep(_cpTaskEnv, _cpTaskEnv.Sequence.Length + 1); // +1 for start bit
            CollectionAssert.AreEqual(new double[] { 0, 1, 0, 0 }, _cpTaskEnv.PerformAction(_dummyAction), "Delimiter bit");
        }

        [TestMethod]
        public void TestScoreStrictCloseToTarget()
        {
            _cpTaskEnv = CreateEnvironment(FitnessFunction.StrictCloseToTarget);
            _cpTaskEnv.ResetAll();
            // All values are correct
            double[][] actions = Utilities.DeepCopy(_cpTaskEnv.Sequence);
            double score = CalcScore(actions);
            Assert.AreEqual(_cpTaskEnv.CurrentScore, _cpTaskEnv.MaxScore, "Max score assert");

            // All values are wrong
            _cpTaskEnv.ResetAll();
            actions = Utilities.DeepCopy(_cpTaskEnv.Sequence);
            for (int i = 0; i < actions.Length; i++)
            {
                for (int j = 0; j < actions[i].Length; j++)
                {
                    if (actions[i][j] == 1) actions[i][j] = 0;
                    else actions[i][j] = 1;
                }
            }
            Assert.AreEqual(_cpTaskEnv.CurrentScore, 0, "Min score assert");
        }

        [TestMethod]
        public void TestScoreClosestBinary()
        {
            _cpTaskEnv = CreateEnvironment(FitnessFunction.ClosestBinary);
            _cpTaskEnv.ResetAll();
            // All values are correct
            double[][] actions = new double[_cpTaskEnv.Sequence.Length][];
            for (int i = 0; i < _cpTaskEnv.Sequence.Length; i++)
            {
                actions[i] = new double[_cpTaskEnv.Sequence[i].Length];
                for (int j = 0; j < _cpTaskEnv.Sequence[i].Length; j++)
                {
                    if (_cpTaskEnv.Sequence[i][j] == 1) actions[i][j] = 0.6;
                    else actions[i][j] = 0.4;
                }
            }
            double score = CalcScore(actions);
            Assert.AreEqual(score, _cpTaskEnv.MaxScore, "Max score assert");

            // All values are wrong
            _cpTaskEnv.ResetAll();
            actions = new double[_cpTaskEnv.Sequence.Length][];
            for (int i = 0; i < _cpTaskEnv.Sequence.Length; i++)
            {
                actions[i] = new double[_cpTaskEnv.Sequence[i].Length];
                for (int j = 0; j < _cpTaskEnv.Sequence[i].Length; j++)
                {
                    if (_cpTaskEnv.Sequence[i][j] == 1) actions[i][j] = 0.4;
                    else actions[i][j] = 0.6;
                }
            }
            score = CalcScore(actions);
            Assert.AreEqual(score, 0, "Min score assert");
        }

        private double CalcScore(double[][] actions)
        {
            FastForwardToStep(_cpTaskEnv, _cpTaskEnv.Sequence.Length + 2); // +2 for start and delimiter bit
            for (int i = 0; i < _cpTaskEnv.Sequence.Length; i++)
            {
                double[] result = { 0, 0, 0, 0 };
                CollectionAssert.AreEqual(result, _cpTaskEnv.PerformAction(actions[i]), $"Sequence read {i}");
            }
            return _cpTaskEnv.CurrentScore;
        }
    }
    }
