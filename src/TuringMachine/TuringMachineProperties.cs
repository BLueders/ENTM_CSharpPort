using SharpNeat.Domains;
using System;
using System.Xml;

namespace ENTM.TuringMachine
{
    public class TuringMachineProperties
    {
        
        public int M { get; }
        public int N { get; }
        public int ShiftLength { get; }
        public ShiftMode ShiftMode { get; }
        public bool Enabled { get; }
        public int Heads { get; }

        public TuringMachineProperties(XmlElement xmlConfig)
        {
            M = XmlUtils.GetValueAsInt(xmlConfig, "M");
            N = XmlUtils.TryGetValueAsInt(xmlConfig, "N") ?? -1;
            ShiftLength = XmlUtils.GetValueAsInt(xmlConfig, "ShiftLength");
            string shiftModeStr = XmlUtils.TryGetValueAsString(xmlConfig, "ShiftMode");
            ShiftMode = shiftModeStr == null ? ShiftMode.Multiple : (ShiftMode) Enum.Parse(typeof(ShiftMode), shiftModeStr);
            Enabled = XmlUtils.TryGetValueAsBool(xmlConfig, "Enabled") ?? true;
            Heads = XmlUtils.TryGetValueAsInt(xmlConfig, "Heads") ?? 1;
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
    }

    public enum ShiftMode
    {
        Multiple,
        Single
    }
}
