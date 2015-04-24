using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;

using System.Windows.Forms;

namespace living_log_cli
{
    public class KeyboardLogger : Logger
    {
        public class KeyboardKeyData : IData
        {
            public Keys Key;
            public KeyboardKeyData(KeyEventArgsExt e) { Key = e.KeyCode; }
            public override string ToString() { return Key.ToString(); }

            protected KeyboardKeyData() { }
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                Keys k;
                if (!System.Enum.TryParse(s, out k)) return false;

                result = new KeyboardKeyData() { Key = k };
                return true;
            }
        }

        public class KeyboardPressData : IData
        {
            public char Character;
            public KeyboardPressData(KeyPressEventArgsExt e) { Character = e.KeyChar; }
            public override string ToString() { return Character.ToString(); }

            protected KeyboardPressData() { }
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                char c;
                if (!char.TryParse(s, out c)) return false;

                result = new KeyboardPressData() { Character = c };
                return true;
            }
        }

        private KeyboardHookListener m_keyboard;

        public KeyboardLogger()
        {
            m_keyboard = new KeyboardHookListener(new GlobalHooker());
            m_keyboard.KeyUp += (s, e) => { if (Enabled) Invoke(Categories.Keyboard_KeyUp, new KeyboardKeyData(e as KeyEventArgsExt)); };
            m_keyboard.KeyDown += (s, e) => { if (Enabled) Invoke(Categories.Keyboard_KeyDown, new KeyboardKeyData(e as KeyEventArgsExt)); };
            m_keyboard.KeyPress += (s, e) => { if (Enabled) Invoke(Categories.Keyboard_KeyPress, new KeyboardPressData(e as KeyPressEventArgsExt)); };
            m_keyboard.Enabled = true;

            m_enabled = false;
        }

        protected bool m_enabled;
        public override bool Enabled
        {
            get { return m_enabled; }
            set { m_enabled = value; }
        }
    }
}
