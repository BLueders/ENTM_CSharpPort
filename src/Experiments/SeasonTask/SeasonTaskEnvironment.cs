using System.Collections.Generic;
using System.Linq;
using ENTM.Base;
using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.SeasonTask
{
    abstract class SeasonTaskEnvironment : BaseEnvironment
    {
        // Describes how many repetitions of each season there will be
        public int Years;

        // How many different seasons are there each year
        public int Seasons;

        // How many different days are there each season
        public int DaysMin;
        public int DaysMax;
        protected int _days;

        // How many different foods there are to select from each season
        protected readonly int _foodTypes;
        protected List<int> _poisonousFoodTypes;
        List<int> _poisonFoodShufflePositions;

        // How many of the foods are poisonous each season
        protected int _poisonFoods;

        // Current time step
        protected int _step;

        // Current fitness score
        protected double _score;

        // How often will the poisonous food type change during one sequence
        protected int _poisonousTypeChanges;

        protected int _specificPoisonousTypeChange;

        // will not score the first day of each season in the first year
        protected bool _ignoreFirstDayOfSeasonInFirstYear;

        // do we give reinforcment learning punishment and reward on food items that are not eaten?
        protected bool _feedbackOnIgnoredFood;

        protected readonly double _fitnessFactor;

        public Food[] Sequence { get; protected set; }

        public int SequenceLength => Years * Seasons * _days * _foodTypes;

        public override double CurrentScore => _score * _fitnessFactor;

        public override double MaxScore
        {
            get
            {
                double maxScore = _fitnessFactor * _foodTypes * _days * Seasons * Years; // regular max score
                maxScore -= _poisonFoodShufflePositions.Count * _foodTypes; // dont score days where the food associations are shuffled
                if (_ignoreFirstDayOfSeasonInFirstYear)
                {
                    // do not score first day of each season of the first year, this is where the food is encountered the first time and for learning
                    return maxScore - (_fitnessFactor * Seasons * _foodTypes);
                }
                return maxScore;
            }
        }

        public override int NoveltyVectorLength => Years * Seasons * DaysMax * _foodTypes;

        public override int NoveltyVectorDimensions => 1;

        public override int MinimumCriteriaLength => 0;

        // jk. do not score first day of each season of the first year. There we encounter each food the first time and cant know if its poisonous.
        protected bool ScoreThisStep(int step)
        {
            bool isFirstDay = step < _foodTypes * _days * Seasons && step % (_foodTypes * _days) < _foodTypes;
            bool isShuffleDay = _poisonFoodShufflePositions.Count != 0 && _poisonFoodShufflePositions.Contains(step / _foodTypes);
            return !((_ignoreFirstDayOfSeasonInFirstYear && isFirstDay) || isShuffleDay);
        }

        public override double NormalizedScore => CurrentScore / MaxScore;

        public override bool IsTerminated => _step >= TotalTimeSteps;

        public override int RandomSeed { get; set; }

        public override IController Controller { get; set; }
        // to deterime if the food was eaten (>0.5) or not (<0.5)
        public override int OutputCount => _foodTypes * Seasons + 2;
        // one input for each food type each season type 
        // + one reward and + one punishing input to determine if a healthy or poisonous food was eaten.
        public override int InputCount => 1;

        public override double[] InitialObservation => GetOutput(0, -1);

        public SeasonTaskEnvironment(SeasonTaskProperties props)
        {
            Years = props.Years;
            Seasons = props.Seasons;
            DaysMin = props.DaysMin;
            DaysMax = props.DaysMax;
            _foodTypes = props.FoodTypes;
            _fitnessFactor = props.FitnessFactor;
            _poisonFoods = props.PoisonFoods;
            _ignoreFirstDayOfSeasonInFirstYear = props.IgnoreFirstDayOfSeasonInFirstYear;
            _poisonousTypeChanges = props.PoisonousTypeChanges;
            RandomSeed = props.RandomSeed;
            _feedbackOnIgnoredFood = props.FeedbackOnIgnoredFood;
            _specificPoisonousTypeChange = props.SpecificPoisonousTypeChange;
        }

        public override void ResetAll()
        {
            Debug.DLogHeader("SEASON TASK RESET ALL", true);

            ResetRandom();
        }

        public override void ResetIteration()
        {
            Debug.DLogHeader("SEASON TASK NEW ITERATION", true);

            _days = SealedRandom.Next(DaysMin, DaysMax + 1);
            CreateSequence();

            Debug.DLog($"{"Years:",-16} {Years}" +
            $"\n{"Seasons:",-16} {Seasons}" +
            $"\n{"DaysMin:",-16} {DaysMin}" +
            $"\n{"DaysMax:",-16} {DaysMax}" +
            $"\n{"Foods:",-16} {_foodTypes}" +
            $"\n{"Max score:",-16} {MaxScore}", true);

            //Debug.Log($"Sequence:\n{Utilities.ToString(Sequence)}", true);

            _step = 1;
            _score = 0d;
        }

        public abstract override double[] PerformAction(double[] action);
        protected abstract double[] GetOutput(int step, double evaluation);

        // To eat or not to eat, that is the question
        protected double Evaluate(double eatVal, int currentFoodIndex)
        {
            // record novelty vector
            if (NoveltySearch.ScoreNovelty)
            {
                NoveltySearch.NoveltyVectors[currentFoodIndex][0] = eatVal;
            }
            const double tolerance = 0.3;

            if (currentFoodIndex < 0)
            {
                return 0;
            }

            // TODO make a more sophisticated score function
            if (eatVal > (1 - tolerance) && !Sequence[currentFoodIndex].IsPoisonous)
            {
                return 1;
            }
            if (eatVal < tolerance && Sequence[currentFoodIndex].IsPoisonous)
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
            _poisonFoodShufflePositions = GetPoisonousFoodShufflePositions();

            int nextShuffleDayIndex = 0;

            for (int i = 0; i < Years; i++)
            {
                for (int j = 0; j < Seasons; j++)
                {
                    for (int k = 0; k < _days; k++)
                    {
                        int currentDayIndex = i * Seasons * _days * _foodTypes + j * _days * _foodTypes + k * _foodTypes;
                        // shuffle dem poisons
                        if (_poisonFoodShufflePositions.Count != 0 && k >= _poisonFoodShufflePositions[nextShuffleDayIndex])
                        {
                            _poisonousFoodTypes = GetPoisonousFoodTypes();
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

        private List<int> GetPoisonousFoodShufflePositions()
        {
            List<int> shufflePositions = new List<int>();
            // If we have a specific date specified, just use this to shuffle the food items around.
            if (_specificPoisonousTypeChange != -1)
            {
                shufflePositions.Add(_specificPoisonousTypeChange);
                return shufflePositions;
            }

            if (_poisonousTypeChanges == 0)
                return shufflePositions;

            // this will not happen on the first, the second or the last day, that would be pointless.
            int[] allPositions = new int[_days - 3];

            // fill array with all foodTypes
            for (int i = 0; i < allPositions.Length; i++)
            {
                allPositions[i] = i + 2;
            }

            // shuffle
            allPositions = allPositions.OrderBy(x => SealedRandom.Next()).ToArray();
            // use first x as random positions to change 
            for (int k = 0; k < _poisonousTypeChanges; k++)
            {
                shufflePositions.Add(allPositions[k]);
            }
            // sort ascending
            shufflePositions = new List<int>(shufflePositions.OrderBy(x => x));
            return shufflePositions;
        }

        private List<int> GetPoisonousFoodTypes()
        {
            List<int> pFoodTypes = new List<int>();
            for (int i = 0; i < Seasons; i++)
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
