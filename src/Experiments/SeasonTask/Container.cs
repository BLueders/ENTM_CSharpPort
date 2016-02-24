using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.Experiments.SeasonsTask
{
    internal struct Year
    {
        public Season[] Seasons;
    }

    internal struct Season
    {
        public int Iteration;
        public int Type;
        public Day[] Days;
    }

    internal struct Day
    {
        public int Iteration;
        public Food Food;
    }

    internal struct Food
    {
        public bool IsPoisonous;
        public int Type;
    }

    internal enum SeasonType
    {
        Summer = 0,
        Winter = 1,
        Spring = 2,
        Autumn = 3
    }

    internal enum FruitType
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
