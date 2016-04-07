using System;
using System.Collections.Generic;
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
                
                // Write key size (M) + 2 (write interp and content jump) + shifts - for each head (probably only one)
                return (_turingMachineProps.M + 2 + shifts) * _turingMachineProps.Heads;
            }
        }

        // Read key for the turing machine for each head
        public override int ControllerOutputCount => _turingMachineProps.M * _turingMachineProps.Heads;

        public override FitnessInfo Evaluate(TuringController controller, int iterations, bool record)
        {
            if (controller == null) throw new ArgumentNullException("Controller was null");

            Utility.Debug.DLogHeader("STARTING EVAULATION", true);

            double totalScore = 0;

            double[][] noveltyVectors = null;

            if (NoveltySearchEnabled)
            {
                noveltyVectors = new double[iterations][];
            }

            // Iteration loop
            for (int i = 0; i < iterations; i++)
            {
                Utility.Debug.DLogHeader($"EVALUATION ITERATION {i}", true);

                Reset();

                double[] enviromentOutput = Environment.InitialObservation;

                if (record)
                {
                    Recorder = new Recorder();
                    Recorder.Start();

                    controller.TuringMachine.RecordTimeSteps = true;
                    Environment.RecordTimeSteps = true;

                    Recorder.Record(Environment.InitialTimeStep, controller.TuringMachine.InitialTimeStep);
                }

                // Environment loop
                while (!Environment.IsTerminated)
                {
                    // Activate the controller with the environment output. 
                    // The turing controller will handle the turing machine I/O
                    double[] environmentInput = controller.ActivateNeuralNetwork(enviromentOutput);

                    // Activate the environment with the output from the controller (NN)
                    enviromentOutput = Environment.PerformAction(environmentInput);

                    if (record)
                    {
                        Recorder.Record(Environment.PreviousTimeStep, controller.TuringMachine.PreviousTimeStep);
                    }
                }

                totalScore += Environment.NormalizedScore;

                if (NoveltySearchEnabled)
                {
                    noveltyVectors[i] = Controller.NoveltyVector;   
                }

                Utility.Debug.DLog($"EVALUATION Total Score: {totalScore}, Iteration Score: {Environment.CurrentScore}", true);
            }

            // Calculate the total normalized score (0-1)
            double environmentScore = Math.Max(0d, totalScore / iterations);

            AuxFitnessInfo[] noveltyScore = null;

            if (NoveltySearchEnabled)
            {
                // Calculate average novelty vector
                double[] totals = new double[NoveltyVectorLength];

                for (int i = 0; i < noveltyVectors.Length; i++)
                {
                    for (int j = 0; j < NoveltyVectorLength; j++)
                    {
                        totals[j] += noveltyVectors[i][j];
                    }
                }

                noveltyScore = new AuxFitnessInfo[NoveltyVectorLength];

                for (int i = 0; i < NoveltyVectorLength; i++)
                {
                    noveltyScore[i] = new AuxFitnessInfo(null, totals[i] / (double) iterations);
                }
            }

            return new FitnessInfo(environmentScore, noveltyScore);
        }
    }
}
