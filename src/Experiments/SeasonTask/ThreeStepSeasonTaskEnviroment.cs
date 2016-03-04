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
    class ThreeStepSeasonTaskEnviroment : SeasonTaskEnvironment
    {
       
        public ThreeStepSeasonTaskEnviroment(SeasonTaskProperties props) : base(props)
        {

        }

        public override int TotalTimeSteps => Sequence.Length * 3;

        public override double[] PerformAction(double[] action)
        {
            Debug.DLogHeader("SEASON TASK START", true);
            Debug.DLog($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.DLog($"{"Step:",-16} {_step}", true);
            double thisScore = 0;
            double[] observation = new double[0];
            int task = _step % 3; // 0 = food step, 1 = eat step, 2 = reward step
            switch (task)
            {
                case 0:
                    Debug.DLog("Task: Food Step", true);
                    observation = GetOutput(_step, -1);
                    break;
                case 1:
                    Debug.DLog("Task: Eat Step", true);
                    observation = GetOutput(_step, -1);
                    break;
                case 2:
                    Debug.DLog("Task: Reward Step", true);
                    double eatVal = action[0];
                    thisScore = Evaluate(eatVal, (_step / 3) - 1);
                    observation = GetOutput(_step, thisScore);
                    if (!IsFirstDayOfSeasonInFirstYear((_step / 3) - 1))
                    {
                        _score += thisScore;
                    }
                    Debug.DLog($"{"Eating:",-16} {eatVal}" +
                                $"\n{"Poisonous:",-16} {Sequence[_step - 1].IsPoisonous}" +
                                $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                                $"\n{"Total Score:",-16} {_score.ToString("F4")} / {_step - 1}" +
                                $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);
                    break;
                default:
                    break;
            }

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
    }
}
