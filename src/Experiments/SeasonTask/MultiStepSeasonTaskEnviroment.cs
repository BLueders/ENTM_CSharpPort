using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.SeasonTask
{
    class MultiStepSeasonTaskEnviroment : SeasonTaskEnvironment
    {

        public MultiStepSeasonTaskEnviroment(SeasonTaskProperties props, int stepsPerFood) : base(props)
        {
            StepNum = stepsPerFood;
        }

        protected int StepNum;

        public override int TotalTimeSteps => Sequence.Length * StepNum;

        public override int MaxTimeSteps => Years * Seasons * DaysMax * _foodTypes * StepNum;

        public override double[] PerformAction(double[] action)
        {
            Debug.DLogHeader("SEASON TASK START", true);
            Debug.DLog($"{"Action:",-16} {Utilities.ToString(action, "f4")}", true);
            Debug.DLog($"{"Step:",-16} {_step}", true);
            double thisEvaluation = 0;
            double[] observation;

            int task = _step % StepNum;
            if (task == StepNum - 1)
            {
                Debug.DLog($"Task: Reward Step", true);
                double eatVal = action[0];
                thisEvaluation = Evaluate(eatVal, (_step / StepNum));
                observation = GetOutput(_step, thisEvaluation);
                if (!ScoreThisStep((_step / StepNum)))
                {
                    thisEvaluation = 0;
                }
                _score += thisEvaluation;
                Debug.DLog($"{"Eating:",-16} {eatVal}" +
                           $"\n{"Poisonous:",-16} {Sequence[(_step / 3) - 1].IsPoisonous}" +
                           $"\n{"Score:",-16} {thisEvaluation.ToString("F4")}" +
                           $"\n{"Total Score:",-16} {_score.ToString("F4")} / {(_step / 3) - 1}" +
                           $"\n{"Max Score:",-16} {Sequence.Length.ToString("F4")}", true);
            }
            else
            {
                Debug.DLog($"Task: Normal Step {(_step / StepNum) - 1}", true);
                observation = GetOutput(_step, -1);
            }

            Debug.DLogHeader("SEASON TASK END", true);

            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, observation, thisEvaluation);
            }

            _step++;
            return observation;
        }

        protected override double[] GetOutput(int step, double evaluation)
        {
            double[] observation = new double[OutputCount];

            Food currentFood = Sequence[step / StepNum];
            observation[currentFood.Type] = 1; // return the current food

            int task = step % StepNum; //when 3: 0 = food step, 1 = eat step, 2 = reward step, when more ??
            if (task == StepNum - 1)
            {
                //// vanilla reward and punishment
                if (_feedbackOnIgnoredFood)
                {
                    if (evaluation < 0.0001)
                        //if(SealedRandom.Next(0,2) > 0)
                        observation[observation.Length - 1] = 1;
                    else
                        observation[observation.Length - 2] = 1;
                }
                // punish only when poisonous is eaten, reward only when nutritios is eaten
                else {
                    if (evaluation < 0.0001 && currentFood.IsPoisonous)
                    {
                        observation[observation.Length - 1] = 1;
                    }
                    if (evaluation > 0.99 && !currentFood.IsPoisonous)
                    {
                        observation[observation.Length - 2] = 1;
                    }
                }
            }
            return observation;
        }
    }
}
