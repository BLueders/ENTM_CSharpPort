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
        public enum FoodSteps
        {
            One, Two, Three
        }

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
            StepsPerFood = (FoodSteps)Enum.Parse(typeof(FoodSteps), XmlUtils.GetValueAsString(xmlConfig, "StepsPerFood"));
        }

        public int Iterations { get; set; }
        public double FitnessFactor { get; set; }
        public int RandomSeed { get; set; }

        public int Years { get; set; }
        public int Seasons { get; set; }
        public int Days { get; set; }
        public int FoodTypes { get; set; }
        public int PoisonFoods { get; set; }
        public FoodSteps StepsPerFood { get; set; }
    }
}
