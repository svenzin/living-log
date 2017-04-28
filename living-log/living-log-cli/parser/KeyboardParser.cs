using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace living_log_cli.parser
{
    public class KeyboardParser
    {
        public class KeyData : IData
        {
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                Keys k;
                if (!System.Enum.TryParse(s, out k)) return false;

                result = new KeyboardLogger.KeyboardKeyData() { Key = k };
                return true;
            }
        }

        public class PressData : IData
        {
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                char c;
                if (!char.TryParse(s, out c)) return false;

                result = new KeyboardLogger.KeyboardPressData() { Character = c };
                return true;
            }
        }
    }
}
