using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace living_log_cli.parser
{
    class MouseParser
    {
        public class ButtonData
        {
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                MouseButtons b;
                if (!System.Enum.TryParse(s, out b)) return false;

                result = new MouseLogger.MouseButtonData() { Button = b };
                return true;
            }
        }

        public class WheelData
        {
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                int d;
                if (!int.TryParse(s, out d)) return false;

                result = new MouseLogger.MouseWheelData() { Delta = d };
                return true;
            }
        }

        public class MoveData
        {
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                var items = s.Split(' ');
                if (items.Length != 2) return false;

                int x;
                if (!int.TryParse(items[0], out x)) return false;

                int y;
                if (!int.TryParse(items[1], out y)) return false;

                result = new MouseLogger.MouseMoveData() { X = x, Y = y };
                return true;
            }
        }
    }
}
