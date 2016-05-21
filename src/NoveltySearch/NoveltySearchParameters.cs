using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.NoveltySearch
{
    public class NoveltySearchParameters
    {
        public bool Enabled;
        public NoveltyVectorMode VectorMode;
        public double PMin;
        public bool EnableArchiveComparison;
        public int ArchiveLimit;
        public int MaxNoveltySearchGenerations;
        public double PMinLowerThreshold;
        public double ObjectiveScoreThreshold;
        public double PMinAdjustUp;
        public double PMinAdjustDown;
        public int AdditionsPMinAdjustUp;
        public int GenerationsPMinAdjustDown;
        public int K;
        public int ReportInterval;
        public double MinimumCriteriaReadWriteLowerThreshold;
        public double ObjectiveFactorExponent;

        public static NoveltySearchParameters ReadXmlProperties(XmlElement xmlConfig)
        {
            NoveltySearchParameters props = new NoveltySearchParameters();

            props.Enabled = XmlUtils.GetValueAsBool(xmlConfig, "Enabled");
            props.PMin = XmlUtils.GetValueAsDouble(xmlConfig, "PMin");
            props.EnableArchiveComparison = XmlUtils.TryGetValueAsBool(xmlConfig, "EnableArchiveComparison") ?? true;
            props.ArchiveLimit = XmlUtils.GetValueAsInt(xmlConfig, "ArchiveLimit");
            props.MaxNoveltySearchGenerations = XmlUtils.GetValueAsInt(xmlConfig, "MaxNoveltySearchGenerations");
            props.PMinLowerThreshold = XmlUtils.TryGetValueAsDouble(xmlConfig, "PMinLowerThreshold") ?? -1;
            props.ObjectiveScoreThreshold = XmlUtils.TryGetValueAsDouble(xmlConfig, "ObjectiveScoreThreshold") ?? .9;
            props.PMinAdjustUp = XmlUtils.GetValueAsDouble(xmlConfig, "PMinAdjustUp");
            props.PMinAdjustDown = XmlUtils.GetValueAsDouble(xmlConfig, "PMinAdjustDown");
            props.AdditionsPMinAdjustUp = XmlUtils.GetValueAsInt(xmlConfig, "AdditionsPMinAdjustUp");
            props.GenerationsPMinAdjustDown = XmlUtils.GetValueAsInt(xmlConfig, "GenerationsPMinAdjustDown");
            props.K = XmlUtils.GetValueAsInt(xmlConfig, "K");
            props.ReportInterval = XmlUtils.GetValueAsInt(xmlConfig, "ReportInterval");
            props.MinimumCriteriaReadWriteLowerThreshold = XmlUtils.GetValueAsDouble(xmlConfig, "MinimumCriteriaReadWriteLowerThreshold");
            props.VectorMode = (NoveltyVectorMode) Enum.Parse(typeof(NoveltyVectorMode), XmlUtils.TryGetValueAsString(xmlConfig, "NoveltyVector") ?? "WritePattern");
            props.ObjectiveFactorExponent = XmlUtils.TryGetValueAsDouble(xmlConfig, "ObjectiveFactorExponent") ?? 0d;

            return props;
        }
    }

    public enum NoveltyVectorMode
    {
        WritePattern,
        ReadContent,
        WritePatternAndInterp,
        ShiftJumpInterp,
        EnvironmentAction
    }
}
