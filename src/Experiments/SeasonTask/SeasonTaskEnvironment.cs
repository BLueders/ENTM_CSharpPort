using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.SeasonTask;
using ENTM.Replay;

namespace ENTM.Experiments.SeasonsTask
{
    class SeasonTaskEnvironment : IEnvironment
    {
        // Describes how many repetitions of each season there will be
        private int _years;

        // How many different seasons are there each year
        private int _seasonTypes;

        // How many different foods there are to select from each season
        private int _foodTypes;

        // How many of the foods are poisonous each season
        private int _poisonFoods;

        // Current time step
        private int _step;

        // Current fitness score
        private double _score;

        private Season[] _sequence;
        private double _fitnessFactor;

        public SeasonTaskEnvironment(SeasonTaskProperties _seasonTaskProps)
        {

        }

        public Season[] Sequence => _sequence;

        public bool RecordTimeSteps { get; set; }
        public EnvironmentTimeStep InitialTimeStep { get; }
        public EnvironmentTimeStep PreviousTimeStep { get; }

        public IController Controller { get; set; }
        // one input for each food type each season type 
        // + one reward and + one punishing input to determine if a healthy or poisonous food was eaten.
        public int InputCount => _foodTypes*_seasonTypes + 2;
        // to deterime if the food was eaten (>0.5) or not (<0.5)
        public int OutputCount => 1;

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public double[] InitialObservation { get; }
        public double[] PerformAction(double[] action)
        {
            throw new NotImplementedException();
        }

        public double CurrentScore => _score * _fitnessFactor; // * ((double)_foodTypes * _seasonTypes * _years / (double)_sequence.Length);

        public double MaxScore => _fitnessFactor * _foodTypes * _seasonTypes * _years;

        public double NormalizedScore => CurrentScore / MaxScore;

        public bool IsTerminated { get; }
        public int TotalTimeSteps { get; }
    }
}
