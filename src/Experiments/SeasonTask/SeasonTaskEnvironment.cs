using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Experiments.SeasonTask;
using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.SeasonTask
{
    abstract class SeasonTaskEnvironment : BaseEnvironment
    {
        // Describes how many repetitions of each season there will be
        protected readonly int _years;

        // How many different seasons are there each year
        protected readonly int _seasons;

        // How many different days are there each season
        protected readonly int _days;

        // How many different foods there are to select from each season
        protected readonly int _foodTypes;
        protected List<int> _poisonousFoodTypes;

        // How many of the foods are poisonous each season
        protected int _poisonFoods;

        // Current time step
        protected int _step;

        // Current fitness score
        protected double _score;

        public Food[] Sequence { get; protected set; }

        protected readonly double _fitnessFactor;

        public override double CurrentScore => _score * _fitnessFactor; // * ((double)_foodTypes * _seasons * _years / (double)_sequence.Length);

        // do not score first day of each season of the first year, this is where the food is encountered the first time and for learning
        public override double MaxScore => _fitnessFactor * _foodTypes * _days * _seasons * _years - (_fitnessFactor * _seasons * _foodTypes);

        // jk. do not score first day of each season of the first year. There we encounter each food the first time and cant know if its poisonous.
        protected bool IsFirstDayOfSeasonInFirstYear(int step)
        {
            return step < _foodTypes*_days*_seasons && step%(_foodTypes*_days) < _foodTypes;
        }

        public override double NormalizedScore => CurrentScore / MaxScore;

        public override bool IsTerminated => _step >= TotalTimeSteps;
        public override int TotalTimeSteps => Sequence.Length * 3;
        public override int RandomSeed { get; set; }

        public override IController Controller { get; set; }
        // to deterime if the food was eaten (>0.5) or not (<0.5)
        public override int OutputCount => _foodTypes * _seasons + 2;
        // one input for each food type each season type 
        // + one reward and + one punishing input to determine if a healthy or poisonous food was eaten.
        public override int InputCount => 1;

        public override double[] InitialObservation => GetOutput(0, -1);

        public SeasonTaskEnvironment(SeasonTaskProperties props)
        {
            _years = props.Years;
            _seasons = props.Seasons;
            _days = props.Days;
            _foodTypes = props.FoodTypes;
            _fitnessFactor = props.FitnessFactor;
            _poisonFoods = props.PoisonFoods;
            RandomSeed = props.RandomSeed;

        }

        public override void ResetAll()
        {
            Debug.LogHeader("SEASON TASK RESET ALL", true);

            ResetRandom();
        }

        public override void ResetIteration()
        {

            Debug.LogHeader("SEASON TASK NEW ITERATION", true);

            CreateSequence();

            Debug.Log($"{"Years:",-16} {_years}" +
            $"\n{"Seasons:",-16} {_seasons}" +
            $"\n{"Days:",-16} {_days}" +
            $"\n{"Foods:",-16} {_foodTypes}, Poisonous: {Utilities.ToString(_poisonousFoodTypes)}" +
            $"\n{"Max score:",-16} {MaxScore}", true);

            //Debug.Log($"Sequence:\n{Utilities.ToString(Sequence)}", true);

            _step = 1;
            _score = 0d;
        }

        public abstract override double[] PerformAction(double[] action);
        protected abstract double[] GetOutput(int step, double evaluation);

        protected double Evaluate(double eatVal, int step)
        {
            const double tolerance = 0.3;

            if (step < 0)
            {
                return 0;
            }

            // TODO make a more sophisticated score function
            if (eatVal > (1 - tolerance) && !Sequence[step].IsPoisonous)
            {
                return 1;
            }
            if (eatVal < tolerance && Sequence[step].IsPoisonous)
            {
                return 1;
            }
            return 0;
        }
       
        protected void CreateSequence()
        {
            // determine the poisonous food types of this iteration for each season
            _poisonousFoodTypes = new List<int>();
            for (int i = 0; i < _seasons; i++)
            {
                int[] allFoodTypes = new int[_foodTypes];
                // fill array with all foodTypes
                for (int j = 0; j < allFoodTypes.Length; j++)
                {
                    allFoodTypes[j] = j + i * _foodTypes;
                }
                // shuffle
                allFoodTypes = allFoodTypes.OrderBy(x => SealedRandom.Next()).ToArray();
                // use first half as random poisonous foods
                for (int k = 0; k < _poisonFoods; k++)
                {
                    _poisonousFoodTypes.Add(allFoodTypes[k]);
                }
            }

            // create the actual sequence
            Sequence = new Food[_years * _seasons * _days * _foodTypes];
            for (int i = 0; i < _years; i++)
            {
                for (int j = 0; j < _seasons; j++)
                {
                    for (int k = 0; k < _days; k++)
                    {
                        // create array of foods
                        Food[] foods = new Food[_foodTypes];
                        for (int l = 0; l < _foodTypes; l++)
                        {
                            Food food = new Food();
                            food.Type = j * _foodTypes + l;
                            food.IsPoisonous = _poisonousFoodTypes.Contains(food.Type);
                            foods[l] = food;
                        }
                        // shuffle foods
                        foods = foods.OrderBy(x => SealedRandom.Next()).ToArray();
                        // copy to sequence
                        for (int l = 0; l < _foodTypes; l++)
                        {
                            Sequence[i * _seasons * _days * _foodTypes + j * _days * _foodTypes + k * _foodTypes + l] = foods[l];
                        }
                    }
                }
            }
        }

        #region RecordTimesteps
        public override bool RecordTimeSteps { get; set; }
        protected EnvironmentTimeStep _prevTimeStep;
        public override EnvironmentTimeStep InitialTimeStep
        {
            get
            {
                return _prevTimeStep = new EnvironmentTimeStep(new double[InputCount], GetOutput(0, -1), _score);
            }
        }
        public override EnvironmentTimeStep PreviousTimeStep => _prevTimeStep;
        #endregion
    }
}
