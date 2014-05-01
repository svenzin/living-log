using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

namespace living_log_cli
{
    public interface Data { string ToString(); }
    public class MouseButtonData : Data
    {
        public MouseButtons Button;
        public MouseButtonData(MouseEventExtArgs e) { Button = e.Button; }
        public override string ToString() { return Button.ToString(); }
    }
    public class MouseWheelData : Data
    {
        public int Delta;
        public MouseWheelData(MouseEventExtArgs e) { Delta = e.Delta; }
        public override string ToString() { return Delta.ToString(); }
    }
    public class MouseMoveData : Data
    {
        public int X;
        public int Y;
        public MouseMoveData(MouseEventExtArgs e) { X = e.X; Y = e.Y; }
        public override string ToString() { return "(" + X.ToString() + ", " + Y.ToString() + ")"; }
    }
    public class KeyboardKeyData : Data
    {
        public Keys Key;
        public KeyboardKeyData(KeyEventArgsExt e) { Key = e.KeyCode; }
        public override string ToString() { return Key.ToString(); }
    }
    public class KeyboardPressData : Data
    {
        public char Character;
        public KeyboardPressData(KeyPressEventArgsExt e) { Character = e.KeyChar; }
        public override string ToString() { return Character.ToString(); }
    }
    public class Activity
    {
        public long Timestamp;
        public string Name;
        public Data Info;
    }
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            Program p = new Program();
            System.Windows.Forms.Application.Run();
        }

        Program()
        {
            m_activityList = new List<Activity>();

            m_mouse = new MouseHookListener(new GlobalHooker());
            m_mouse.MouseMoveExt += OnMouseMove;
            m_mouse.MouseDownExt += OnMouseDown;
            m_mouse.MouseUp += OnMouseUp;
            m_mouse.MouseClickExt += OnMouseClick;
            m_mouse.MouseDoubleClick += OnMouseDoubleClick;
            m_mouse.MouseWheel += OnMouseWheel;
            m_mouse.Enabled = true;
            
            m_keyboard = new KeyboardHookListener(new GlobalHooker());
            m_keyboard.KeyUp += OnKeyUp;
            m_keyboard.KeyDown += OnKeyDown;
            m_keyboard.KeyPress += OnKeyPress;
            m_keyboard.Enabled = true;

            m_timer = new Timer();
            m_timer.Interval = 1000 * 60; // 1 minute
            m_timer.Tick += OnTick;
            m_timer.Enabled = true;
        }

        private void Act(string name, Data info)
        {
            m_activityList.Add(new Activity() { Timestamp = DateTime.UtcNow.Ticks, Name = name, Info = info });
        }

        private void OnTick(object sender, EventArgs e)
        {
            using (var file = new System.IO.StreamWriter(@"C:\ActivityLog.txt", true))
            {
                m_activityList.ForEach((a) => { file.WriteLine(a.Timestamp.ToString() + " " + a.Name + " " + a.Info.ToString()); });
                m_activityList.Clear();
            }
        }

        private void OnMouseMove(object sender, MouseEventExtArgs e)
        {
            Act("MouseMove", new MouseMoveData(e));
        }
        private void OnMouseDown(object sender, MouseEventExtArgs e)
        {
            Act("MouseDown", new MouseButtonData(e));
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            Act("MouseUp", new MouseButtonData(e as MouseEventExtArgs));
        }
        private void OnMouseClick(object sender, MouseEventExtArgs e)
        {
            Act("MouseClick", new MouseButtonData(e));
        }
        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            Act("MouseDoubleClick", new MouseButtonData(e as MouseEventExtArgs));
        }
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            Act("MouseWheel", new MouseWheelData(e as MouseEventExtArgs));
        }

        private void OnKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            Act("KeyUp", new KeyboardKeyData(e as KeyEventArgsExt));
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Act("KeyDown", new KeyboardKeyData(e as KeyEventArgsExt));
        }
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            Act("KeyPress", new KeyboardPressData(e as KeyPressEventArgsExt));
        }

        Timer m_timer;

        MouseHookListener m_mouse;
        KeyboardHookListener m_keyboard;

        List<Activity> m_activityList;
    }
}
