using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.Experiments.SeasonsTask
{
    struct Season
    {
        public int Iteration;
        public int Type;
        public Day[] Days;
    }

    struct Day
    {
        public int Iteration;
        public Food Food;
    }

    struct Food
    {
        public bool IsPoisonous;
        public int Type;
    }

    enum SeasonType
    {
        Summer = 0,
        Winter = 1,
        Spring = 2,
        Autumn = 3
    }

    enum FruitType
    {
        Apple = 0,
        Berry = 1,
        Cherry = 2,
        Kiwi = 3,
        Mushroom = 4,
        Spinach = 5,
        Pumpkin = 6,
        Wallnut = 7
    }
}
