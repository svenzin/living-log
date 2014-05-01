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
    public interface Category { string ToString(); }
    public class Mouse_Move        : Category { public override string ToString() { return "Mouse_Move";        } }
    public class Mouse_Down        : Category { public override string ToString() { return "Mouse_Down";        } }
    public class Mouse_Up          : Category { public override string ToString() { return "Mouse_Up";          } }
    public class Mouse_Click       : Category { public override string ToString() { return "Mouse_Click";       } }
    public class Mouse_DoubleClick : Category { public override string ToString() { return "Mouse_DoubleClick"; } }
    public class Mouse_Wheel       : Category { public override string ToString() { return "Mouse_Wheel";       } }
    public class Keyboard_KeyDown  : Category { public override string ToString() { return "Keyboard_KeyDown";  } }
    public class Keyboard_KeyUp    : Category { public override string ToString() { return "Keyboard_KeyUp";    } }
    public class Keyboard_KeyPress : Category { public override string ToString() { return "Keyboard_KeyPress"; } }
    public class Activity
    {
        public long Timestamp;
        public Category Type;
        public Data Info;
    }
    class Program
    {
        static void Main(string[] args)
        {
            string filename;
            if (args.Length == 0)
            {
                filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\living-log.log";
            }
            else if (args.Length == 2 && args[0] == "-log")
            {
                filename = args[1];
            }
            else
            {
                Help();
                return;
            }

            Console.WriteLine("Using " + filename + " as log file");

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            Program p = new Program(filename);
            System.Windows.Forms.Application.Run();
        }

        static void Help()
        {
            Console.WriteLine(
@"
living-log-cli

Starts a keyboard and mouse activity logger

Command: living-log-cli [-log LOG]

Options: -log LOG Uses the file LOG as log for the activity
                  Default is ""My Documents""\living-log.log"
            );
        }
        
        Program(string filename)
        {
            m_activityList = new List<Activity>();

            m_mouse = new MouseHookListener(new GlobalHooker());
            m_mouse.MouseMoveExt     += OnMouseMove;
            m_mouse.MouseDownExt     += OnMouseDown;
            m_mouse.MouseUp          += OnMouseUp;
            m_mouse.MouseClickExt    += OnMouseClick;
            m_mouse.MouseDoubleClick += OnMouseDoubleClick;
            m_mouse.MouseWheel       += OnMouseWheel;
            m_mouse.Enabled = true;
            
            m_keyboard = new KeyboardHookListener(new GlobalHooker());
            m_keyboard.KeyUp    += OnKeyUp;
            m_keyboard.KeyDown  += OnKeyDown;
            m_keyboard.KeyPress += OnKeyPress;
            m_keyboard.Enabled = true;

            m_timer = new Timer();
            m_timer.Interval = 1000 * 60; // 1 minute
            m_timer.Tick += OnTick;
            m_timer.Enabled = true;

            m_filename = filename;
        }

        private void Act(Category type, Data info)
        {
            m_activityList.Add(new Activity() { Timestamp = DateTime.UtcNow.Ticks, Type = type, Info = info });
        }

        private void OnTick(object sender, EventArgs e)
        {
            using (var file = new System.IO.StreamWriter(m_filename, true))
            {
                m_activityList.ForEach((a) => { file.WriteLine(a.Timestamp.ToString() + " " + a.Type.ToString() + " " + a.Info.ToString()); });
                m_activityList.Clear();
            }
        }

        private void OnMouseMove       (object sender, MouseEventExtArgs e) { Act(new Mouse_Move(),        new MouseMoveData  (e));                      }
        private void OnMouseDown       (object sender, MouseEventExtArgs e) { Act(new Mouse_Down(),        new MouseButtonData(e));                      }
        private void OnMouseUp         (object sender, MouseEventArgs    e) { Act(new Mouse_Up(),          new MouseButtonData(e as MouseEventExtArgs)); }
        private void OnMouseClick      (object sender, MouseEventExtArgs e) { Act(new Mouse_Click(),       new MouseButtonData(e));                      }
        private void OnMouseDoubleClick(object sender, MouseEventArgs    e) { Act(new Mouse_DoubleClick(), new MouseButtonData(e as MouseEventExtArgs)); }
        private void OnMouseWheel      (object sender, MouseEventArgs    e) { Act(new Mouse_Wheel(),       new MouseWheelData (e as MouseEventExtArgs)); }

        private void OnKeyUp   (object sender, System.Windows.Forms.KeyEventArgs e) { Act(new Keyboard_KeyUp(),    new KeyboardKeyData  (e as KeyEventArgsExt));      }
        private void OnKeyDown (object sender, KeyEventArgs                      e) { Act(new Keyboard_KeyDown(),  new KeyboardKeyData  (e as KeyEventArgsExt));      }
        private void OnKeyPress(object sender, KeyPressEventArgs                 e) { Act(new Keyboard_KeyPress(), new KeyboardPressData(e as KeyPressEventArgsExt)); }

        Timer m_timer;
        string m_filename;

        MouseHookListener m_mouse;
        KeyboardHookListener m_keyboard;

        List<Activity> m_activityList;
    }
}
