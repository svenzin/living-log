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
        }

        public class KeyboardPressData : IData
        {
            public char Character;
            public KeyboardPressData(KeyPressEventArgsExt e) { Character = e.KeyChar; }
            public override string ToString() { return Character.ToString(); }
        }

        private KeyboardHookListener m_keyboard;

        public KeyboardLogger()
        {
            m_keyboard = new KeyboardHookListener(new GlobalHooker());
            m_keyboard.KeyUp += (s, e) => { if (Enabled) Invoke(Categories.Keyboard_KeyUp, new KeyboardKeyData(e as KeyEventArgsExt)); };
            m_keyboard.KeyDown += (s, e) => { if (Enabled) Invoke(Categories.Keyboard_KeyDown, new KeyboardKeyData(e as KeyEventArgsExt)); };
            m_keyboard.KeyPress += (s, e) => { if (Enabled) Invoke(Categories.Keyboard_KeyPress, new KeyboardPressData(e as KeyPressEventArgsExt)); };
            m_keyboard.Enabled = true;
        }

        protected override void Enable() { }
        protected override void Disable() { }
    }
}
