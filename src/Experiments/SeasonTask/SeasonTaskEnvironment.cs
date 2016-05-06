using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Base;
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

        // How often will the poisonous food type change during one sequence
        protected int _poisonousTypeChanges;

        // will not score the first day of each season in the first year
        protected bool _ignoreFirstDayOfSeasonInFirstYear;

        protected readonly double _fitnessFactor;

        public Food[] Sequence { get; protected set; }

        public int SequenceLength => _years * _seasons * _days * _foodTypes;

        public override double CurrentScore => _score * _fitnessFactor;

        public override double MaxScore {
            get
            {
                if (_ignoreFirstDayOfSeasonInFirstYear)
                {
                    // do not score first day of each season of the first year, this is where the food is encountered the first time and for learning
                    return _fitnessFactor * _foodTypes * _days * _seasons * _years - (_fitnessFactor * _seasons * _foodTypes);
                }
                return _fitnessFactor*_foodTypes*_days*_seasons*_years;
            }
        } 

        // jk. do not score first day of each season of the first year. There we encounter each food the first time and cant know if its poisonous.
        protected bool ScoreThisStep(int step)
        {
            bool isFirstDay = step < _foodTypes * _days * _seasons && step % (_foodTypes * _days) < _foodTypes;
            if (isFirstDay)
            {
                int a = 1;
            }
            return !(_ignoreFirstDayOfSeasonInFirstYear && isFirstDay);
        }

        public override double NormalizedScore => CurrentScore / MaxScore;

        public override bool IsTerminated => _step >= TotalTimeSteps;

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
            _ignoreFirstDayOfSeasonInFirstYear = props.IgnoreFirstDayOfSeasonInFirstYear;
            _poisonousTypeChanges = props.PoisonousTypeChanges;
            RandomSeed = props.RandomSeed;

        }

        public override void ResetAll()
        {
            Debug.DLogHeader("SEASON TASK RESET ALL", true);

            ResetRandom();
        }

        public override void ResetIteration()
        {

            Debug.DLogHeader("SEASON TASK NEW ITERATION", true);

            CreateSequence();

            Debug.DLog($"{"Years:",-16} {_years}" +
            $"\n{"Seasons:",-16} {_seasons}" +
            $"\n{"Days:",-16} {_days}" +
            $"\n{"Foods:",-16} {_foodTypes}" +
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
            _poisonousFoodTypes = GetPoisonousFoodTypes();

            // create the actual sequence
            Sequence = new Food[SequenceLength];
            // if we are going to change which foods are poisonous during sequence, this is where
            LinkedList<int> poisonFoodShufflePositions = GetPoisonousFoodShufflePositions(Sequence.Length);

            for (int i = 0; i < _years; i++)
            {
                for (int j = 0; j < _seasons; j++)
                {
                    for (int k = 0; k < _days; k++)
                    {
                        int currentDayIndex = i*_seasons*_days*_foodTypes + j*_days*_foodTypes + k*_foodTypes;
                        // shuffle dem poisons
                        if (poisonFoodShufflePositions.Count != 0 && currentDayIndex >= poisonFoodShufflePositions.First.Value)
                        {
                            _poisonousFoodTypes = GetPoisonousFoodTypes();
                            poisonFoodShufflePositions.RemoveFirst();
                        }
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
                            Sequence[currentDayIndex + l] = foods[l];
                        }
                    }
                }
            }
        }

        private LinkedList<int> GetPoisonousFoodShufflePositions(int sequenceLength)
        {
            LinkedList<int> shufflePositions = new LinkedList<int>();
            int[] allPositions = new int[sequenceLength];
            // fill array with all foodTypes
            for (int j = 0; j < allPositions.Length; j++)
            {
                allPositions[j] = j;
            }
            // shuffle
            allPositions = allPositions.OrderBy(x => SealedRandom.Next()).ToArray();
            // use first x as random positions to change 
            for (int k = 0; k < _poisonousTypeChanges; k++)
            {
                shufflePositions.AddLast(allPositions[k]);
            }
            // sort ascending
            shufflePositions = new LinkedList<int>(shufflePositions.OrderBy(x => x));
            return shufflePositions;
        }

        private List<int> GetPoisonousFoodTypes()
        {
            List<int> pFoodTypes = new List<int>();
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
                    pFoodTypes.Add(allFoodTypes[k]);
                }
            }

            return pFoodTypes;
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
