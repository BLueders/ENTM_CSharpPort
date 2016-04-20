using System;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.Experiments.CopyTask
{
    public class CopyTaskProperties
    {
        private const int DEFAULT_ITERATIONS = 10;
        private const int DEFAULT_SEQUENCE_MAXLENGTH = 10;

        // Evaluation
        public int Iterations;
        public int RandomSeed;

        // Copy task environment
        public int VectorSize;
        public int MaxSequenceLength;
        public FitnessFunction FitnessFunction;
        public LengthRule LengthRule;
        public bool EliminateZeroVectors;

        public CopyTaskProperties(XmlElement xmlConfig)
        {
            Iterations = XmlUtils.TryGetValueAsInt(xmlConfig, "Iterations") ?? DEFAULT_ITERATIONS;
            RandomSeed = XmlUtils.TryGetValueAsInt(xmlConfig, "RandomSeed") ?? 0;
            VectorSize = XmlUtils.GetValueAsInt(xmlConfig, "VectorSize");
            MaxSequenceLength = XmlUtils.TryGetValueAsInt(xmlConfig, "MaxLength") ?? DEFAULT_SEQUENCE_MAXLENGTH;
            LengthRule = (LengthRule)Enum.Parse(typeof(LengthRule), XmlUtils.GetValueAsString(xmlConfig, "LengthRule"));
            FitnessFunction = (FitnessFunction)Enum.Parse(typeof(FitnessFunction), XmlUtils.GetValueAsString(xmlConfig, "FitnessFunction"));
            EliminateZeroVectors = XmlUtils.TryGetValueAsBool(xmlConfig, "EliminateZeroVectors") ?? false;
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
