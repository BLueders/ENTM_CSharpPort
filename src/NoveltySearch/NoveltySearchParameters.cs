﻿using System;
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
        public double PMinAdjustUp;
        public double PMinAdjustDown;
        public int AdditionsPMinAdjustUp;
        public int GenerationsPMinAdjustDown;
        public int K;
        public int ReportInterval;

        public static NoveltySearchParameters ReadXmlProperties(XmlElement xmlConfig)
        {
            NoveltySearchParameters props = new NoveltySearchParameters();

            props.Enabled = XmlUtils.GetValueAsBool(xmlConfig, "Enabled");
            props.PMin = XmlUtils.GetValueAsDouble(xmlConfig, "PMin");
            props.ArchiveLimit = XmlUtils.GetValueAsInt(xmlConfig, "ArchiveLimit");
            props.PMinAdjustUp = XmlUtils.GetValueAsDouble(xmlConfig, "PMinAdjustUp");
            props.PMinAdjustDown = XmlUtils.GetValueAsDouble(xmlConfig, "PMinAdjustDown");
            props.AdditionsPMinAdjustUp = XmlUtils.GetValueAsInt(xmlConfig, "AdditionsPMinAdjustUp");
            props.GenerationsPMinAdjustDown = XmlUtils.GetValueAsInt(xmlConfig, "GenerationsPMinAdjustDown");
            props.K = XmlUtils.GetValueAsInt(xmlConfig, "K");
            props.ReportInterval = XmlUtils.GetValueAsInt(xmlConfig, "ReportInterval");

            return props;
        }
    }
}
