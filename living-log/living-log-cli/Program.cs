using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace living_log_cli
{
    public interface IData
    {
        void WriteText(TextWriter writer);
        void WriteBinary(BinaryWriter writer);
        string ToString();
    }
    public class MouseButtonData : IData
    {
        public MouseButtons Button;
        public MouseButtonData(MouseEventExtArgs e) { Button = e.Button; }
        public override string ToString() { return Button.ToString(); }
        public void WriteText(TextWriter writer) { writer.Write(ToString()); }
        public void WriteBinary(BinaryWriter writer) { writer.Write((int)Button); }
    }
    public class MouseWheelData : IData
    {
        public int Delta;
        public MouseWheelData(MouseEventExtArgs e) { Delta = e.Delta; }
        public override string ToString() { return Delta.ToString(); }
        public void WriteText(TextWriter writer) { writer.Write(ToString()); }
        public void WriteBinary(BinaryWriter writer) { writer.Write(Delta); }
    }
    public class MouseMoveData : IData
    {
        public int X;
        public int Y;
        public MouseMoveData(MouseEventExtArgs e) { X = e.X; Y = e.Y; }
        public override string ToString() { return "(" + X.ToString() + ", " + Y.ToString() + ")"; }
        public void WriteText(TextWriter writer) { writer.Write(ToString()); }
        public void WriteBinary(BinaryWriter writer) { writer.Write(X); writer.Write(Y); }
    }
    public class KeyboardKeyData : IData
    {
        public Keys Key;
        public KeyboardKeyData(KeyEventArgsExt e) { Key = e.KeyCode; }
        public override string ToString() { return Key.ToString(); }
        public void WriteText(TextWriter writer) { writer.Write(ToString()); }
        public void WriteBinary(BinaryWriter writer) { writer.Write((int)Key); }
    }
    public class KeyboardPressData : IData
    {
        public char Character;
        public KeyboardPressData(KeyPressEventArgsExt e) { Character = e.KeyChar; }
        public override string ToString() { return Character.ToString(); }
        public void WriteText(TextWriter writer) { writer.Write(ToString()); }
        public void WriteBinary(BinaryWriter writer) { writer.Write(Character); }
    }
    public class SyncData : IData
    {
        public string Version;
        public long Timestamp;
        public override string ToString() { return Timestamp.ToString() + " " + Version; }
        public void WriteText(TextWriter writer) { writer.Write(ToString()); }
        public void WriteBinary(BinaryWriter writer) { writer.Write(Version); writer.Write(Timestamp); }
    }
    public interface ICategory { int GetId(); string ToString(); }
    public class LivingLog_Startup : ICategory { public int GetId() { return 0; } public override string ToString() { return GetId() + " LivingLog_Startup"; } }
    public class Mouse_Move        : ICategory { public int GetId() { return 1; } public override string ToString() { return GetId() + " Mouse_Move";        } }
    public class Mouse_Down        : ICategory { public int GetId() { return 2; } public override string ToString() { return GetId() + " Mouse_Down";        } }
    public class Mouse_Up          : ICategory { public int GetId() { return 3; } public override string ToString() { return GetId() + " Mouse_Up";          } }
    public class Mouse_Click       : ICategory { public int GetId() { return 4; } public override string ToString() { return GetId() + " Mouse_Click";       } }
    public class Mouse_DoubleClick : ICategory { public int GetId() { return 5; } public override string ToString() { return GetId() + " Mouse_DoubleClick"; } }
    public class Mouse_Wheel       : ICategory { public int GetId() { return 6; } public override string ToString() { return GetId() + " Mouse_Wheel";       } }
    public class Keyboard_KeyDown  : ICategory { public int GetId() { return 7; } public override string ToString() { return GetId() + " Keyboard_KeyDown";  } }
    public class Keyboard_KeyUp    : ICategory { public int GetId() { return 8; } public override string ToString() { return GetId() + " Keyboard_KeyUp";    } }
    public class Keyboard_KeyPress : ICategory { public int GetId() { return 9; } public override string ToString() { return GetId() + " Keyboard_KeyPress"; } }
    public class Activity
    {
        public long Timestamp;
        public ICategory Type;
        public IData Info;

        public void WriteText(TextWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(" ");
            writer.Write(Type.ToString());
            writer.Write(" ");
            Info.WriteText(writer);
        }

        public void WriteBinary(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(Type.GetId());
            Info.WriteBinary(writer);
        }
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

            Act(new LivingLog_Startup(), new SyncData() { Timestamp = DateTime.UtcNow.Ticks, Version = "0.1" });

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

        private void Act(ICategory type, IData info)
        {
            m_activityList.Add(new Activity() { Timestamp = DateTime.UtcNow.Ticks, Type = type, Info = info });
        }

        private void OnTick(object sender, EventArgs e)
        {
            Console.WriteLine("Writing " + m_activityList.Count + " activities");
            using (var file = new BinaryWriter(new FileStream(m_filename, FileMode.Append)))
            {
                WriteBinary(file);
                m_activityList.Clear();
            }
        }

        private void WriteText(TextWriter text)
        {
            m_activityList.ForEach((a) => { a.WriteText(text); text.WriteLine(); });
        }

        private void WriteBinary(BinaryWriter data)
        {
            m_activityList.ForEach((a) => { a.WriteBinary(data); });
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
