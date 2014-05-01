using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace living_log_cli
{
    public interface Data { }
    public class Activity
    {
        public int Timestamp;
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

        private void OnTick(object sender, EventArgs e)
        {
            using (var file = new System.IO.StreamWriter(@"C:\ActivityLog.txt", true))
            {
                m_activityList.ForEach((a) => { file.WriteLine(a.Timestamp.ToString() + " " + a.Name); });
                m_activityList.Clear();
            }
        }

        private void OnMouseMove(object sender, MouseEventExtArgs e)
        {
            var args = e as MouseEventExtArgs;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "MouseMove" });
        }
        private void OnMouseDown(object sender, MouseEventExtArgs e)
        {
            var args = e as MouseEventExtArgs;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "MouseDown" });
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            var args = e as MouseEventExtArgs;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "MouseUp" });
        }
        private void OnMouseClick(object sender, MouseEventExtArgs e)
        {
            var args = e as MouseEventExtArgs;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "MouseClick" });
        }
        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            var args = e as MouseEventExtArgs;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "MouseDoubleClick" });
        }
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var args = e as MouseEventExtArgs;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "MouseWheel" });
        }

        private void OnKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            var args = e as KeyEventArgsExt;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "KeyUp" });
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var args = e as KeyEventArgsExt;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "KeyDown" });
        }
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            var args = e as KeyPressEventArgsExt;
            m_activityList.Add(new Activity() { Timestamp = args.Timestamp, Name = "KeyPress" });
        }

        Timer m_timer;

        MouseHookListener m_mouse;
        KeyboardHookListener m_keyboard;

        List<Activity> m_activityList;
    }
}
