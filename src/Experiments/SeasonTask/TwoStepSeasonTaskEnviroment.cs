using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.SeasonTask
{
    class TwoStepSeasonTaskEnviroment : SeasonTaskEnvironment
    {

        public TwoStepSeasonTaskEnviroment(SeasonTaskProperties props) : base(props)
        {

        }


        public override int TotalTimeSteps => Sequence.Length * 2 + 1; // we have one extra scoring step at the end for the last food eaten

        public override int MaxTimeSteps => SequenceLength * 2 + 1;

        public override double[] PerformAction(double[] action)
        {
            Debug.DLogHeader("SEASON TASK START", true);
            Debug.DLog($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.DLog($"{"Step:",-16} {_step}", true);
            double thisScore = 0;
            double[] observation = new double[OutputCount];
            int task = _step % 2; // 0 = reward / food step, 1 = eat step
            switch (task)
            {
                case 0:
                    Debug.DLog("Task: Reward / Food Step", true);
                    double eatVal = action[0];
                    thisScore = Evaluate(eatVal, (_step - 2) / 2); // we compare against the previos food, therefor - 2
                    observation = GetOutput(_step, thisScore);

                    if (!ScoreThisStep((_step - 2) / 2)) // no scoring here
                    {
                        thisScore = 0;
                    }
                    _score += thisScore;

                    Debug.DLog($"{"Eating:",-16} {eatVal}" +
                                $"\n{"Last Was Poisonous:",-16} {Sequence[(_step - 2) / 2].IsPoisonous}" +
                                $"\n{"Score:",-16} {thisScore.ToString("F4")}" +
                                $"\n{"Total Score:",-16} {_score.ToString("F4")} / {(_step / 2)}" +
                                $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);
                    break;
                case 1:
                    Debug.DLog("Task: Eat Step", true);
                    // send only 0s
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
            int task = step % 2; // 0 = reward / food step, 1 = eat step
            switch (task)
            {
                case 0:
                    if (step != Sequence.Length * 2) // the last step is scoring only
                    {
                        Food currentFood = Sequence[step / 2];
                        observation[currentFood.Type] = 1; // return the current food
                    }
                    if (step != 0) // first step has food only
                    {
                        if (evaluation == 0) // return evaluation from last food
                            observation[observation.Length - 1] = 1;
                        else
                            observation[observation.Length - 2] = 1;
                    }
                    break;
                case 1:
                    break;
                default:
                    break;
            }
            return observation;
        }
    }
}
