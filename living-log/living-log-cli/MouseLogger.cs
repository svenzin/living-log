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
    public class MouseLogger : Logger
    {
        public class MouseButtonData : IData
        {
            public MouseButtons Button;
            public MouseButtonData(MouseEventExtArgs e) { Button = e.Button; }
            public override string ToString() { return Button.ToString(); }

            protected MouseButtonData() { }
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                MouseButtons b;
                if (!System.Enum.TryParse(s, out b)) return false;

                result = new MouseButtonData() { Button = b };
                return true;
            }
        }

        public class MouseWheelData : IData
        {
            public int Delta;
            public MouseWheelData(MouseEventExtArgs e) { Delta = e.Delta; }
            public override string ToString() { return Delta.ToString(); }

            protected MouseWheelData() { }
            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                int d;
                if (!int.TryParse(s, out d)) return false;

                result = new MouseWheelData() { Delta = d };
                return true;
            }
        }

        public class MouseMoveData : IData
        {
            public int X;
            public int Y;
            public MouseMoveData(MouseEventExtArgs e) { X = e.X; Y = e.Y; }
            public override string ToString() { return X.ToString() + " " + Y.ToString(); }

            protected MouseMoveData() { }
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

                result = new MouseMoveData() { X = x, Y = y };
                return true;
            }
        }

        private MouseHookListener m_mouse;

        public MouseLogger()
        {
            m_mouse = new MouseHookListener(new GlobalHooker());
            m_mouse.MouseMoveExt += (s, e) => { if (Enabled) Invoke(Categories.Mouse_Move, new MouseMoveData(e)); };
            m_mouse.MouseDownExt += (s, e) => { if (Enabled)  Invoke(Categories.Mouse_Down, new MouseButtonData(e)); };
            m_mouse.MouseUp += (s, e) => { if (Enabled) Invoke(Categories.Mouse_Up, new MouseButtonData(e as MouseEventExtArgs)); };
            m_mouse.MouseClickExt += (s, e) => { if (Enabled) Invoke(Categories.Mouse_Click, new MouseButtonData(e)); };
            m_mouse.MouseDoubleClick += (s, e) => { if (Enabled)  Invoke(Categories.Mouse_DoubleClick, new MouseButtonData(e as MouseEventExtArgs)); };
            m_mouse.MouseWheel += (s, e) => { if (Enabled)  Invoke(Categories.Mouse_Wheel, new MouseWheelData(e as MouseEventExtArgs)); };
            m_mouse.Enabled = true;

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
