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
        public int Iterations;

        // Copy task environment
        public int VectorSize;
        public int MaxSequenceLength;
        public FitnessFunction FitnessFunction;
        public LengthRule LengthRule;
        public double FitnessFactor;

        public CopyTaskProperties(int iterations, int vectorSize, int maxSequenceLength, FitnessFunction fitnessFunction, LengthRule lengthRule, double fitnessFactor)
        {
            Iterations = iterations;
            VectorSize = vectorSize;
            MaxSequenceLength = maxSequenceLength;
            FitnessFunction = fitnessFunction;
            LengthRule = lengthRule;
            FitnessFactor = fitnessFactor;
        }

        public CopyTaskProperties(XmlElement xmlConfig)
        {
            Iterations = XmlUtils.TryGetValueAsInt(xmlConfig, "Iterations") ?? DEFAULT_ITERATIONS;

            VectorSize = XmlUtils.GetValueAsInt(xmlConfig, "VectorSize");
            MaxSequenceLength = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxLength") ?? DEFAULT_SEQUENCE_MAXLENGTH;
            LengthRule = (LengthRule)Enum.Parse(typeof(LengthRule), XmlUtils.GetValueAsString(xmlConfig, "LengthRule"));
            FitnessFunction = (FitnessFunction)Enum.Parse(typeof(FitnessFunction), XmlUtils.GetValueAsString(xmlConfig, "FitnessFunction"));
            FitnessFactor = XmlUtils.TryGetValueAsDouble(xmlConfig, "FitnessFactor") ?? DEFAULT_FITNESS_FACTOR;
        }

        public CopyTaskProperties()
        {
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
