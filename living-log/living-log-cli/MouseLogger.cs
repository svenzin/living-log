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
        }

        public class MouseWheelData : IData
        {
            public int Delta;
            public MouseWheelData(MouseEventExtArgs e) { Delta = e.Delta; }
            public override string ToString() { return Delta.ToString(); }
        }

        public class MouseMoveData : IData
        {
            public int X;
            public int Y;
            public MouseMoveData(MouseEventExtArgs e) { X = e.X; Y = e.Y; }
            public override string ToString() { return X.ToString() + " " + Y.ToString(); }
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
        }

        protected override void Enable() { }
        protected override void Disable() { }
    }
}
