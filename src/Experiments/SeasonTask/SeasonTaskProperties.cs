using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpNeat.Domains;

namespace ENTM.Experiments.SeasonTask
{
    class SeasonTaskProperties
    {
        private XmlElement xmlConfig;
        private const int DEFAULT_ITERATIONS = 10;
        private const double DEFAULT_FITNESS_FACTOR = 1;

        public SeasonTaskProperties(XmlElement xmlConfig)
        {
            this.xmlConfig = xmlConfig;
            Iterations = XmlUtils.TryGetValueAsInt(xmlConfig, "Iterations") ?? DEFAULT_ITERATIONS;
            FitnessFactor = XmlUtils.TryGetValueAsInt(xmlConfig, "FitnessFactor") ?? DEFAULT_FITNESS_FACTOR;
            RandomSeed = XmlUtils.TryGetValueAsInt(xmlConfig, "RandomSeed") ?? 0;
            Years = XmlUtils.TryGetValueAsInt(xmlConfig, "Years") ?? 0;
            Seasons = XmlUtils.TryGetValueAsInt(xmlConfig, "Seasons") ?? 0;
            Days = XmlUtils.TryGetValueAsInt(xmlConfig, "Days") ?? 0;
            FoodTypes = XmlUtils.TryGetValueAsInt(xmlConfig, "FoodTypes") ?? 0;
            PoisonFoods = XmlUtils.TryGetValueAsInt(xmlConfig, "PoisonFoods") ?? 0;
            PoisonousTypeChanges = XmlUtils.TryGetValueAsInt(xmlConfig, "PoisonousTypeChanges") ?? 0;
            IgnoreFirstDayOfSeasonInFirstYear = XmlUtils.TryGetValueAsBool(xmlConfig, "IgnoreFirstDayOfSeasonInFirstYear") ?? false;
            StepsPerFood = XmlUtils.TryGetValueAsInt(xmlConfig, "StepsPerFood") ?? 3;
            FeedbackOnIgnoredFood = XmlUtils.TryGetValueAsBool(xmlConfig, "FeedbackOnIgnoredFood") ?? true;
        }

        public int Iterations { get; set; }
        public double FitnessFactor { get; set; }
        public int RandomSeed { get; set; }

        public int Years { get; set; }
        public int Seasons { get; set; }
        public int Days { get; set; }
        public int FoodTypes { get; set; }
        public int PoisonFoods { get; set; }
        public int StepsPerFood { get; set; }
        public bool FeedbackOnIgnoredFood { get; set; }

        public int PoisonousTypeChanges;
        public bool IgnoreFirstDayOfSeasonInFirstYear; // this could be chosen not to be scored, as the algorithm cant know the answer here
    }
}
