using System;
using System.Xml;
using ENTM.Replay;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace ENTM.TuringMachine
{
    public abstract class TuringEvaluator<TEnvironment> : BaseEvaluator<TEnvironment, TuringController> where TEnvironment : IEnvironment
    {
        private TuringMachineProperties _turingMachineProps;

        public override void Initialize(XmlElement xmlConfig)
        {
            _turingMachineProps = new TuringMachineProperties(xmlConfig.SelectSingleNode("TuringMachineParams") as XmlElement);
        }

        protected override TuringController NewController()
        {
            return new TuringController(_turingMachineProps);
        }

        public override int ControllerInputCount
        {
            get
            {
                int shifts = 0;
                switch (_turingMachineProps.ShiftMode)
                {
                    case ShiftMode.Multiple:
                        shifts = _turingMachineProps.ShiftLength;
                        break;
                    case ShiftMode.Single:
                        shifts = 1;
                        break;
                }

                // Write key size (M) + 2 (write interp and content jump) + shifts for each head (probably only one)
                return (_turingMachineProps.M + 2 + shifts) * _turingMachineProps.Heads;
            }
        }

        // Read key for the turing machine for each head
        public override int ControllerOutputCount => _turingMachineProps.M * _turingMachineProps.Heads;

        public override FitnessInfo Evaluate(IBlackBox phenome, int iterations, bool record)
        {
            if (phenome == null) Console.WriteLine("Warning! Trying to evalutate null phenome!");

            Utility.Debug.LogHeader("STARTING EVAULATION", true);
            double totalScore = 0;

            Controller.BlackBox = phenome;
            Environment.Controller = Controller;

            AuxFitnessInfo[] noveltyScore = new AuxFitnessInfo[10];

            // For each iteration
            for (int i = 0; i < iterations; i++)
            {
                Utility.Debug.LogHeader($"EVALUATION ITERATION {i}", true);

                Reset();

                double[] enviromentOutput = Environment.InitialObservation;

                if (record)
                {
                    Recorder = new Recorder();
                    Recorder.Start();

                    Controller.TuringMachine.RecordTimeSteps = true;
                    Environment.RecordTimeSteps = true;

                    Recorder.Record(Environment.InitialTimeStep, Controller.TuringMachine.InitialTimeStep);
                }

                while (!Environment.IsTerminated)
                {
                    // Activate the controller with the environment output
                    double[] environmentInput = Controller.ActivateNeuralNetwork(enviromentOutput);

                    // Oh shit, some parallel error happened, bail out. This only occurs in the first generation sometimes
                    if (environmentInput == null) return new FitnessInfo(0, noveltyScore);

                    // Activate the environment with the output from the controller (NN)
                    enviromentOutput = Environment.PerformAction(environmentInput);

                    if (record)
                    {
                        Recorder.Record(Environment.PreviousTimeStep, Controller.TuringMachine.PreviousTimeStep);
                    }
                }

                totalScore += Environment.NormalizedScore;

                Utility.Debug.Log($"EVALUATION Total Score: {totalScore}, Iteration Score: {Environment.CurrentScore}", true);
            }

            // Unregister the phenome
            Controller.BlackBox = null;

            double environmentScore = Math.Max(0d, totalScore / iterations);

            return new FitnessInfo(environmentScore, noveltyScore);
        }
    }
}
