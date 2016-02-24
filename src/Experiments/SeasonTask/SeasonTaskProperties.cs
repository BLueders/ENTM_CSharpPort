using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ENTM.Experiments.SeasonTask
{
    class SeasonTaskProperties
    {
        private XmlElement xmlElement;

        public SeasonTaskProperties(XmlElement xmlElement)
        {
            this.xmlElement = xmlElement;
        }
    }
}
