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
    class SeasonTaskEnvironment : BaseEnvironment
    {
        // Describes how many repetitions of each season there will be
        private readonly int _years;

        // How many different seasons are there each year
        private readonly int _seasons;

        // How many different days are there each season
        private readonly int _days;

        // How many different foods there are to select from each season
        private readonly int _foodTypes;
        private List<int> _poisonousFoodTypes;

        // How many of the foods are poisonous each season
        private int _poisonFoods;

        // Current time step
        private int _step;

        // Current fitness score
        private double _score;

        public Food[] Sequence { get; private set; }

        private readonly double _fitnessFactor;

        public override double CurrentScore => _score * _fitnessFactor; // * ((double)_foodTypes * _seasons * _years / (double)_sequence.Length);

        // do not score first day of each season of the first year, this is where the food is encountered the first time and for learning
        public override double MaxScore => _fitnessFactor * _foodTypes * _days * _seasons * _years - (_fitnessFactor * _seasons * _foodTypes);

        public override double NormalizedScore => CurrentScore / MaxScore;

        public override bool IsTerminated => _step >= TotalTimeSteps;
        public override int TotalTimeSteps => Sequence.Length * 3;
        public override int RandomSeed { get; }

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
        }

        public override void ResetIteration()
        {
            base.ResetIteration();

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

        public override double[] PerformAction(double[] action)
        {
            Debug.LogHeader("SEASON TASK START", true);
            Debug.Log($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.Log($"{"Step:",-16} {_step}", true);
            double thisScore = 0;
            double[] observation = new double[0];
            int task = _step % 3; // 0 = food step, 1 = eat step, 2 = reward step
            switch (task)
            {
                case 0:
                    Debug.Log("Task: Food Step", true);
                    observation = GetOutput(_step, -1);
                    break;
                case 1:
                    Debug.Log("Task: Eat Step", true);
                    observation = GetOutput(_step, -1);
                    break;
                case 2:
                    Debug.Log("Task: Reward Step", true);
                    double eatVal = action[0];
                    thisScore = Evaluate(eatVal, (_step - 1) / 3);
                    observation = GetOutput(_step, thisScore);
                    _score += thisScore;
                    Debug.Log($"{"Eating:",-16} {eatVal}" +
                                $"\n{"Poisonous:",-16} {Sequence[_step].IsPoisonous}" +
                                $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                                $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step - 1}" +
                                $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);
                    break;
                default:
                    break;
            }

            Debug.LogHeader("SEASON TASK END", true);

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, observation, thisScore);
            }

            _step++;
            return observation;
        }

        /* // original implementation with only one kind of task, eat and reward in one step
        public override double[] PerformAction(double[] action)
        {
            Debug.LogHeader("SEASON TASK START", true);
            Debug.Log($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.Log($"{"Step:",-16} {_step}", true);
            double thisScore = 0;
            double[] observation = GetOutput(_step);
            if (_step != 0)
            {
                double eatVal = action[0];
                thisScore = Evaluate(eatVal, _step);
                _score += thisScore;
                Debug.Log($"{"Eating:",-16} {eatVal}" +
                            $"\n{"Poisonous:",-16} {Sequence[_step].IsPoisonous}" +
                            $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                            $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step - 1}" +
                            $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);
            }

            Debug.LogHeader("SEASON TASK END", true);

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, observation, thisScore);
            }

            _step++;
            return observation;
        }
        */
        private double Evaluate(double eatVal, int step)
        {
            const double tolerance = 0.3;

            // self explanatory lol
            // jk. do not score first day of each season of the first year. There we encounter each food the first time and cant know if its poisonous.
            if (step < _foodTypes * _days * _seasons && step % (_foodTypes * _days) < _foodTypes)
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

        private double[] GetOutput(int step, double evaluation)
        {
            double[] observation = new double[OutputCount];
            int task = step % 3; // 0 = food step, 1 = eat step, 2 = reward step
            switch (task)
            {
                case 0:
                    Food currentFood = Sequence[step / 3];
                    observation[currentFood.Type] = 1; // return the current food
                    break;
                case 1:
                    return observation;
                case 2:
                    if (evaluation == 0)
                        observation[observation.Length - 1] = 1;
                    else
                        observation[observation.Length - 2] = 1;
                    break;
                default:
                    break;
            }
            return observation;
        }

        private void CreateSequence()
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
        private EnvironmentTimeStep _prevTimeStep;
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
