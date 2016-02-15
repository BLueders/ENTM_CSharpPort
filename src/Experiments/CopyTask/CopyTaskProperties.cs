using System;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskProperties
    {
        private const int DEFAULT_ITERATIONS = 10;
        private const int DEFAULT_SEQUENCE_MAXLENGTH = 10;
        private const double DEFAULT_FITNESS_FACTOR = 10;

        public CopyTaskProperties(XmlElement xmlConfig)
        {
            Iterations = XmlUtils.TryGetValueAsInt(xmlConfig, "Iterations") ?? DEFAULT_ITERATIONS;

            M = XmlUtils.GetValueAsInt(xmlConfig, "M");
            N = XmlUtils.TryGetValueAsInt(xmlConfig, "N") ?? -1;
            ShiftLength = XmlUtils.GetValueAsInt(xmlConfig, "ShiftLength");
            string shiftModeStr = XmlUtils.TryGetValueAsString(xmlConfig, "ShiftMode");
            ShiftMode = shiftModeStr == null ? ShiftMode.Multiple : (ShiftMode)Enum.Parse(typeof(ShiftMode), shiftModeStr);
            Enabled = XmlUtils.TryGetValueAsBool(xmlConfig, "Enabled") ?? true;
            Heads = XmlUtils.TryGetValueAsInt(xmlConfig, "Heads") ?? 1;

            VectorSize = XmlUtils.GetValueAsInt(xmlConfig, "VectorSize");
            MaxSequenceLength = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxLength") ?? DEFAULT_SEQUENCE_MAXLENGTH;
            LengthRule = (LengthRule)Enum.Parse(typeof(LengthRule), XmlUtils.GetValueAsString(xmlConfig, "LengthRule"));
            FitnessFunction = (FitnessFunction)Enum.Parse(typeof(FitnessFunction), XmlUtils.GetValueAsString(xmlConfig, "FitnessFunction"));
            FitnessFactor = XmlUtils.TryGetValueAsDouble(xmlConfig, "FitnessFactor") ?? DEFAULT_FITNESS_FACTOR;
        }

        // Evaluation
        public int Iterations { get; }

        // Turing machine
        public int M { get; }
        public int N { get; }
        public int ShiftLength { get; }
        public ShiftMode ShiftMode { get; }
        public bool Enabled { get; }
        public int Heads { get; }

        // Copy task environment
        public int VectorSize { get; }
        public int MaxSequenceLength { get; }
        public FitnessFunction FitnessFunction { get; }
        public LengthRule LengthRule { get; }
        public double FitnessFactor { get; }
    }

    public enum ShiftMode
    {
        Multiple,
        Single
    }

    public enum FitnessFunction
    {
        StrictCloseToTarget = 0,
        PartialScore = 1,
        Emilarity = 2,
        ClosestBinary = 3,
        CompleteBinary = 4
    }

    public enum LengthRule
    {
        Fixed = 0,
        Random = 1
    }
}
