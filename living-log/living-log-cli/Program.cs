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
    public class Constants
    {
        public static long TicksPerMs = TimeSpan.TicksPerMillisecond;
        public static int DumpDelayInMs = 60 * 1000; // 1 minute
        public static int SyncDelayInMs = 60 * 60 * 1000; // 1 hour
        public static string SyncFormat = "yyyy-MM-dd_HH:mm:ss.fff";
    }
    public struct Timestamp
    {
        public long Milliseconds;

        public Timestamp(long milliseconds) { Milliseconds = milliseconds; }
        public Timestamp(DateTime time) : this(time.Ticks / Constants.TicksPerMs) { }

        public override string ToString() { return Milliseconds.ToString(); }

        public static Timestamp operator +(Timestamp a, Timestamp b) { return new Timestamp(a.Milliseconds + b.Milliseconds); }
        public static Timestamp operator -(Timestamp a, Timestamp b) { return new Timestamp(a.Milliseconds - b.Milliseconds); }
    }
    public interface IData
    {
        string ToString();
    }
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
    public class SyncData : IData
    {
        public string Version;
        public DateTime Timestamp;
        public override string ToString() { return Timestamp.ToString(Constants.SyncFormat) + " " + Version; }
    }
    
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public int GetId() { return Id; }
        public override string ToString() { return Id.ToString() + " " + Name; }

        public static Category LivingLog_Startup = new Category() { Id = 0, Name = "LivingLog_Startup" };
        public static Category LivingLog_Sync    = new Category() { Id =10, Name = "LivingLog_Sync"    };
        public static Category Mouse_Move        = new Category() { Id = 1, Name = "Mouse_Move"        };
        public static Category Mouse_Down        = new Category() { Id = 2, Name = "Mouse_Down"        };
        public static Category Mouse_Up          = new Category() { Id = 3, Name = "Mouse_Up"          };
        public static Category Mouse_Click       = new Category() { Id = 4, Name = "Mouse_Click"       };
        public static Category Mouse_DoubleClick = new Category() { Id = 5, Name = "Mouse_DoubleClick" };
        public static Category Mouse_Wheel       = new Category() { Id = 6, Name = "Mouse_Wheel"       };
        public static Category Keyboard_KeyDown  = new Category() { Id = 7, Name = "Keyboard_KeyDown"  };
        public static Category Keyboard_KeyUp    = new Category() { Id = 8, Name = "Keyboard_KeyUp"    };
        public static Category Keyboard_KeyPress = new Category() { Id = 9, Name = "Keyboard_KeyPress" };
    }
    public class Activity
    {
        public Timestamp Timestamp;
        public Category Type;
        public IData Info;
    }
    class Program
    {
        static void Main(string[] args)
        {
            string filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\living-log.log";
            int index = 0;
            while (index < args.Length)
            {
                if (index + 2 <= args.Length)
                {
                    switch (args[index])
                    {
                        case "-log":
                            filename = args[index + 1];
                            break;
                        case "-delay":
                            Constants.DumpDelayInMs = 1000 * Convert.ToInt32(args[index + 1]);
                            break;
                        case "-sync":
                            Constants.SyncDelayInMs = 1000 * Convert.ToInt32(args[index + 1]);
                            break;
                        default:
                            Help();
                            return;
                    }
                    index += 2;
                }
                else
                {
                    Help();
                    return;
                }
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

Command: living-log-cli [-log LOG -delay DELAY -sync DELAY]

Options: -log LOG     Uses the file LOG as log for the activity
                      Default is ""My Documents""\living-log.log
         -delay DELAY Delay in seconds between each dump to the log file
                      Default is each minute (60s)
         -sync DELAY  Delay in seconds between each sync message in the dump
                      Default is each hour (3600s)
"
            );
        }
        
        Program(string filename)
        {
            m_activityList = new List<Activity>();

            m_previous = new Timestamp(DateTime.UtcNow);
            Act(Category.LivingLog_Startup, new SyncData() { Timestamp = DateTime.UtcNow, Version = "1" });

            m_mouse = new MouseHookListener(new GlobalHooker());
            m_mouse.MouseMoveExt     += (s, e) => { Act(Category.Mouse_Move,        new MouseMoveData  (e)); };
            m_mouse.MouseDownExt     += (s, e) => { Act(Category.Mouse_Down,        new MouseButtonData(e)); };
            m_mouse.MouseUp          += (s, e) => { Act(Category.Mouse_Up,          new MouseButtonData(e as MouseEventExtArgs)); };
            m_mouse.MouseClickExt    += (s, e) => { Act(Category.Mouse_Click,       new MouseButtonData(e)); };
            m_mouse.MouseDoubleClick += (s, e) => { Act(Category.Mouse_DoubleClick, new MouseButtonData(e as MouseEventExtArgs)); };
            m_mouse.MouseWheel       += (s, e) => { Act(Category.Mouse_Wheel,       new MouseWheelData (e as MouseEventExtArgs)); };
            m_mouse.Enabled = true;
            
            m_keyboard = new KeyboardHookListener(new GlobalHooker());
            m_keyboard.KeyUp    += (s, e) => { Act(Category.Keyboard_KeyUp,    new KeyboardKeyData  (e as KeyEventArgsExt)); };
            m_keyboard.KeyDown  += (s, e) => { Act(Category.Keyboard_KeyDown,  new KeyboardKeyData  (e as KeyEventArgsExt)); };
            m_keyboard.KeyPress += (s, e) => { Act(Category.Keyboard_KeyPress, new KeyboardPressData(e as KeyPressEventArgsExt)); };
            m_keyboard.Enabled = true;

            m_dumpTimer = new Timer();
            m_dumpTimer.Interval = Constants.DumpDelayInMs;
            m_dumpTimer.Tick += OnTick;
            m_dumpTimer.Enabled = true;

            m_syncTimer = new Timer();
            m_syncTimer.Interval = Constants.SyncDelayInMs;
            m_syncTimer.Tick += (s, e) => { Act(Category.LivingLog_Sync, new SyncData() { Timestamp = DateTime.UtcNow, Version = "1" }); };
            m_syncTimer.Enabled = true;

            m_filename = filename;
        }

        private void Act(Category type, IData info)
        {
            m_activityList.Add(new Activity() { Timestamp = new Timestamp(DateTime.UtcNow), Type = type, Info = info });
        }

        private void OnTick(object sender, EventArgs e)
        {
            Console.WriteLine("Writing " + m_activityList.Count + " activities");
            using (var writer = new StreamWriter(new FileStream(m_filename, FileMode.Append)))
            {
                WriteText(writer);
                m_activityList.Clear();
            }
        }

        private void WriteText(TextWriter writer)
        {
            m_activityList.ForEach((a) =>
            {
                writer.Write(a.Timestamp - m_previous);
                writer.Write(" ");
                writer.Write(a.Type.Id);
                writer.Write(" ");
                writer.Write(a.Info.ToString());
                writer.WriteLine();

                m_previous = a.Timestamp;
            });
        }

        Timer m_syncTimer;
        Timer m_dumpTimer;
        string m_filename;

        MouseHookListener m_mouse;
        KeyboardHookListener m_keyboard;

        Timestamp m_previous;
        List<Activity> m_activityList;
    }
}
