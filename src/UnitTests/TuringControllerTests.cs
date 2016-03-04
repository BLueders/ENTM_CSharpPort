using System;
using System.Text;
using System.Collections.Generic;
using ENTM.TuringMachine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    /// <summary>
    /// Summary description for TuringControllerTests
    /// </summary>
    [TestClass]
    public class TuringControllerTests
    {
        private TuringController _controller;

        [TestInitialize()]
        public void Before()
        {
            TuringMachineProperties props = new TuringMachineProperties
            {
                M = 5,
                N = -1,
                ShiftLength = 3,
                ShiftMode = ShiftMode.Multiple,
                Enabled = true,
                Heads = 1
            };
            int vectorSize = 2;
            int startAndDelimiter = 2;
            int interp = 1;
            int contentJump = 1;
            int shift = 3;
            BlackBoxDummy blackBoxDummy = new BlackBoxDummy(vectorSize + startAndDelimiter + props.M,               // input 4 environment + 5 turing machine = 9
                                                            vectorSize + props.M + interp + contentJump + shift);   // output = 2 environment + 10 turing machine = 12
            double[][] outputValues =
            {
                new double[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12}
            };
            _controller = new TuringController(props);
            _controller.Phenome = blackBoxDummy;
        }

        [TestMethod]
        public void TestActivateNN()
        {
            double[][] outputValues =
            {
                new double[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12}
            };

            ((BlackBoxDummy)_controller.Phenome).SetOutputValues(outputValues);
            double[] result = _controller.ActivateNeuralNetwork(new double[4]);
            CollectionAssert.AreEqual(outputValues[0], result, "Output from NN activation");
        }
    }
}
