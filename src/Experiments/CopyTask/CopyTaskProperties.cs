using System;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskProperties
    {
        private const int DEFAULT_ITERATIONS = 10;
        private const int DEFAULT_SEQUENCE_MAXLENGTH = 10;
        private const double DEFAULT_FITNESS_FACTOR = 1;

        // Evaluation
        public int Iterations { get; }

        // Copy task environment
        public int VectorSize { get; }
        public int MaxSequenceLength { get; }
        public FitnessFunction FitnessFunction { get; }
        public LengthRule LengthRule { get; }
        public double FitnessFactor { get; }

        public CopyTaskProperties(XmlElement xmlConfig)
        {
            Iterations = XmlUtils.TryGetValueAsInt(xmlConfig, "Iterations") ?? DEFAULT_ITERATIONS;

            VectorSize = XmlUtils.GetValueAsInt(xmlConfig, "VectorSize");
            MaxSequenceLength = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxLength") ?? DEFAULT_SEQUENCE_MAXLENGTH;
            LengthRule = (LengthRule)Enum.Parse(typeof(LengthRule), XmlUtils.GetValueAsString(xmlConfig, "LengthRule"));
            FitnessFunction = (FitnessFunction)Enum.Parse(typeof(FitnessFunction), XmlUtils.GetValueAsString(xmlConfig, "FitnessFunction"));
            FitnessFactor = XmlUtils.TryGetValueAsDouble(xmlConfig, "FitnessFactor") ?? DEFAULT_FITNESS_FACTOR;
        }
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
