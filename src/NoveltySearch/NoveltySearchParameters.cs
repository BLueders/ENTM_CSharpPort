using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.NoveltySearch
{
    class NoveltySearchParameters
    {
        public bool Enabled;
        public double PMin;
        public int ArchiveLimit;


        public static NoveltySearchParameters ReadXmlProperties(XmlElement xmlConfig)
        {
            NoveltySearchParameters props = new NoveltySearchParameters();

            props.Enabled = XmlUtils.GetValueAsBool(xmlConfig, "Enabled");
            props.PMin = XmlUtils.GetValueAsDouble(xmlConfig, "PMin");
            props.ArchiveLimit = XmlUtils.GetValueAsInt(xmlConfig, "ArchiveLimit");

            return props;
        }
    }
}
