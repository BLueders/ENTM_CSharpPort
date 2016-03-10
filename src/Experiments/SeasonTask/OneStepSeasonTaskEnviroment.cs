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
    class OneStepSeasonTaskEnviroment : SeasonTaskEnvironment
    {

        public OneStepSeasonTaskEnviroment(SeasonTaskProperties props) : base(props)
        {

        }

        public override int TotalTimeSteps => Sequence.Length;

        public override double[] PerformAction(double[] action)
        {
            Debug.DLogHeader("SEASON TASK START", true);
            Debug.DLog($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.DLog($"{"Step:",-16} {_step}", true);
            double thisScore = 0;
            double[] observation = new double[OutputCount];

            Debug.DLog("Task: Reward / Food Step", true);
            double eatVal = action[0];
            thisScore = Evaluate(eatVal, _step - 1);
            observation = GetOutput(_step, thisScore);

            if (IsFirstDayOfSeasonInFirstYear(_step)) // no scoring here
            {
                thisScore = 0;
            }
            _score += thisScore;

            Debug.DLog($"{"Eating:",-16} {eatVal}" +
                        $"\n{"Last Was Poisonous:",-16} {Sequence[_step - 1].IsPoisonous}" +
                        $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                        $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step}" +
                        $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);



            Debug.DLogHeader("SEASON TASK END", true);

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, observation, thisScore);
            }

            _step++;
            return observation;
        }

        protected override double[] GetOutput(int step, double evaluation)
        {
            double[] observation = new double[OutputCount];

            Food currentFood = Sequence[step];
            observation[currentFood.Type] = 1; // return the current food

            if (evaluation == 0) // return evaluation from last food
                observation[observation.Length - 1] = 1;
            else
                observation[observation.Length - 2] = 1;

            return observation;
        }
    }
}
