using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public static class Tools
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }

        public static string ToHumanString(int value) { return ToHumanString((long)value); }
        public static string ToHumanString(long value)
        {
            if (value < 1000) return value.ToString();

            string[] units = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
            int i = 0;
            while (value >= 1000000)
            {
                value = value / 1000;
                ++i;
            }
            double d = value;
            if (d >= 1000)
            {
                d = d / 1000;
                ++i;
            }
            if (d >= 100) return d.ToString("F0") + units[i];
            else if (d >= 10) return d.ToString("F0") + units[i];
            return d.ToString("F1") + units[i];
        }
    }
}
