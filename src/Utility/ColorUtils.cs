﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.Utility
{
    public static class ColorUtils
    {
        /// <summary>
        /// Create a Color from HSV values
        /// </summary>
        /// <param name="hue">0-360</param>
        /// <param name="saturation">0-1</param>
        /// <param name="value">0-1</param>
        /// <returns></returns>
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        /// <summary>
        /// Create a greyscale color from value v
        /// </summary>
        /// <param name="v">0-1</param>
        /// <returns></returns>
        public static Color BlackAndWhite(double v)
        {
            byte c = (byte) (Byte.MaxValue * v);
            return Color.FromArgb(c, c, c);
        }
    }
}
