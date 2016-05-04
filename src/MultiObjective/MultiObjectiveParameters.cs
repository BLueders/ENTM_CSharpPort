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
        public int GeneticDiversityK;

        public static MultiObjectiveParameters ReadXmlProperties(XmlElement xmlConfig)
        {
            MultiObjectiveParameters props = new MultiObjectiveParameters();

            props.Enabled = XmlUtils.TryGetValueAsBool(xmlConfig, "Enabled") ?? false;
            props.GeneticDiversityK = XmlUtils.TryGetValueAsInt(xmlConfig, "GeneticDiversityK") ?? 10;

            return props;
        }
    }
}
