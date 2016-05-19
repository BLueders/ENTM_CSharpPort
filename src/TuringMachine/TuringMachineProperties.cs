using SharpNeat.Domains;
using System;
using System.Xml;

namespace ENTM.TuringMachine
{
    public class TuringMachineProperties
    {
        public bool Enabled;
        public int M;
        public int N;
        public int Heads;
        public int ShiftLength;
        public ShiftMode ShiftMode;
        public WriteMode WriteMode;
        public double MinSimilarityToJump;
        public double InitalValue;
        public bool InitalizeWithGradient;
        public double DidWriteThreshold;
        public bool UseMemoryExpandLocation;

        public TuringMachineProperties(XmlElement xmlConfig)
        {
            Enabled = XmlUtils.TryGetValueAsBool(xmlConfig, "Enabled") ?? true;
            M = XmlUtils.GetValueAsInt(xmlConfig, "M");
            N = XmlUtils.TryGetValueAsInt(xmlConfig, "N") ?? -1;
            Heads = XmlUtils.TryGetValueAsInt(xmlConfig, "Heads") ?? 1;
            ShiftLength = XmlUtils.GetValueAsInt(xmlConfig, "ShiftLength");
            MinSimilarityToJump = XmlUtils.TryGetValueAsDouble(xmlConfig, "MinSimilarityToJump") ?? 0;

            string shiftModeStr = XmlUtils.TryGetValueAsString(xmlConfig, "ShiftMode");
            ShiftMode = shiftModeStr == null ? ShiftMode.Multiple : (ShiftMode) Enum.Parse(typeof(ShiftMode), shiftModeStr);

            string writeModeStr = XmlUtils.TryGetValueAsString(xmlConfig, "WriteMode");
            WriteMode = writeModeStr == null ? WriteMode.Interpolate : (WriteMode) Enum.Parse(typeof(WriteMode), writeModeStr);

            InitalizeWithGradient = XmlUtils.TryGetValueAsBool(xmlConfig, "InitalizeWithGradient") ?? false;

            InitalValue = XmlUtils.TryGetValueAsDouble(xmlConfig, "InitalValue") ?? 0;

            UseMemoryExpandLocation = XmlUtils.TryGetValueAsBool(xmlConfig, "UseMemoryExpandLocation") ?? false;
            DidWriteThreshold = XmlUtils.TryGetValueAsDouble(xmlConfig, "DidWriteThreshold") ?? 0.9d;
        }

        public TuringMachineProperties(int m, int n, int shiftLength, ShiftMode shiftMode, bool enabled, int heads)
        {
            M = m;
            N = n;
            ShiftLength = shiftLength;
            ShiftMode = shiftMode;
            Enabled = enabled;
            Heads = heads;
        }

        public TuringMachineProperties()
        {
        }

    }

    public enum ShiftMode
    {
        Multiple,
        Single
    }

    public enum WriteMode
    {
        Interpolate,
        Overwrite
    }
}
