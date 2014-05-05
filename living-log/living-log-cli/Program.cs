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
    class Program
    {
        #region Console exit handler
        
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        public static bool SetExitHandler(HandlerRoutine handler) { return SetConsoleCtrlHandler(handler, true); }
       
        #endregion

        #region Command line arguments handling
       
        private static void Help()
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

        private static bool TryProcessingArguments(string[] args)
        {
            int index = 0;
            while (index < args.Length)
            {
                if (index + 2 <= args.Length)
                {
                    switch (args[index])
                    {
                        case "-log":
                            Constants.LogFilename = args[index + 1];
                            break;
                        case "-delay":
                            Constants.DumpDelayInMs = 1000 * Convert.ToInt32(args[index + 1]);
                            break;
                        case "-sync":
                            Constants.SyncDelayInMs = 1000 * Convert.ToInt32(args[index + 1]);
                            break;
                        default:
                            return false;
                    }
                    index += 2;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
       
        #endregion

        static void Main(string[] args)
        {
            if (!TryProcessingArguments(args))
            {
                Help();
                return;
            }

            Console.WriteLine("Using " + Constants.LogFilename + " as log file"); 

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            Program p = new Program(Constants.LogFilename);
            SetExitHandler((t) => { return p.ForceExit(); });
            System.Windows.Forms.Application.Run();
        }


        public class LivingLogger : Logger
        {
            public class SyncData : IData
            {
                public string Version;
                public DateTime Timestamp;
                public override string ToString() { return Timestamp.ToString(Constants.SyncFormat) + " " + Version; }
            }
            
            public LivingLogger(int syncDelay)
            {
                m_sync = new Timer();
                m_sync.Interval = syncDelay;
                m_sync.Tick += (s, e) => { Invoke(Categories.LivingLog_Sync, new SyncData() { Timestamp = DateTime.UtcNow, Version = "1" }); };
                m_sync.Enabled = true;
            }

            private Timer m_sync;
        }

        Program(string filename)
        {
            m_activityList = new List<Activity>();

            m_previous = new Timestamp(DateTime.UtcNow);
            Act(Categories.LivingLog_Startup, new LivingLogger.SyncData() { Timestamp = DateTime.UtcNow, Version = "1" });

            m_mouse = new MouseLogger();
            m_mouse.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_keyboard = new KeyboardLogger();
            m_keyboard.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_living = new LivingLogger(Constants.SyncDelayInMs);
            m_living.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_dumpTimer = new Timer();
            m_dumpTimer.Interval = Constants.DumpDelayInMs;
            m_dumpTimer.Tick += OnTick;
            m_dumpTimer.Enabled = true;

            m_filename = filename;
        }

        private bool ForceExit()
        {
            m_dumpTimer.Enabled = false;

            Act(Categories.LivingLog_Exit, new LivingLogger.SyncData() { Timestamp = DateTime.UtcNow, Version = "1" });
            OnTick(null, null);
            return true;
        }

        private void Act(Category type, IData info)
        {
            lock (locker)
            {
                m_activityList.Add(new Activity() { Timestamp = new Timestamp(DateTime.UtcNow), Type = type, Info = info });
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            Console.WriteLine("Writing " + m_activityList.Count + " activities");
            lock (locker)
            {
                using (var writer = new StreamWriter(new FileStream(m_filename, FileMode.Append)))
                {
                    WriteText(writer);
                    m_activityList.Clear();
                }
            }
        }

        private Timestamp m_previous;
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

        Timer m_dumpTimer;
        string m_filename;

        MouseLogger m_mouse;
        KeyboardLogger m_keyboard;
        LivingLogger m_living;

        List<Activity> m_activityList;

        private object locker = new object();
    }
}
