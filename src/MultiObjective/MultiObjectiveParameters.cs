using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.NoveltySearch
{
    public class MultiObjectiveParameters
    {
        public bool Enabled;
        public bool GeneticDiversityEnabled;
        public int GeneticDiversityK;
        public bool RejectSimilarBehaviours;
        public double RejectSimilarThreshold;

        public static MultiObjectiveParameters ReadXmlProperties(XmlElement xmlConfig)
        {
            MultiObjectiveParameters props = new MultiObjectiveParameters();

            props.Enabled = XmlUtils.TryGetValueAsBool(xmlConfig, "Enabled") ?? false;
            props.GeneticDiversityEnabled = XmlUtils.TryGetValueAsBool(xmlConfig, "GeneticDiversityEnabled") ?? true;
            props.GeneticDiversityK = XmlUtils.TryGetValueAsInt(xmlConfig, "GeneticDiversityK") ?? 10;
            props.RejectSimilarBehaviours = XmlUtils.TryGetValueAsBool(xmlConfig, "RejectSimilarBehaviours") ?? false;
            props.RejectSimilarThreshold = XmlUtils.TryGetValueAsDouble(xmlConfig, "RejectSimilarThreshold") ?? 0.001d;

            return props;
        }
    }
}