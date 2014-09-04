using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;

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
        private static HandlerRoutine exitHandler = null;
        public static bool SetExitHandler(HandlerRoutine handler)
        {
            exitHandler = handler;
            return SetConsoleCtrlHandler(exitHandler, true);
        }
       
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
            SetExitHandler((t) =>
            {
                p.ForceExit();
                return true;
            });

            //var messagePump = new Thread(() => { p.REPL(Console.In, Console.Out); }) { IsBackground = true };
            //messagePump.Start();
            new Thread(() => { p.REPL(Console.In, Console.Out); }) { IsBackground = true }.Start();
            //p.Enabled = false;
            //p.Enabled = true;
            System.Windows.Forms.Application.Run();
        }

        private TextReader Input;
        private TextWriter Output;
        void REPL(TextReader input, TextWriter output)
        {
            Input = input;
            Output = output;

            string command = string.Empty;
            do
            {
                command = Input.ReadLine().Trim();
                if (command.Equals("pause"))
                {
                    if (Enabled)
                    {
                        Enabled = false;
                        Output.WriteLine("Paused");
                    }
                }
                else if (command.Equals("resume"))
                {
                    if (!Enabled)
                    {
                        Enabled = true;
                        Output.WriteLine("Resumed");
                    }
                }
            } while (!command.Equals("exit"));
        }

        Program(string filename)
        {
            m_activityList = new List<Activity>();

            m_previous = new Timestamp(DateTime.UtcNow);

            m_living = new LivingLogger(Constants.SyncDelayInMs);
            m_living.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_mouse = new MouseLogger();
            m_mouse.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_keyboard = new KeyboardLogger();
            m_keyboard.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_dumpTimer = new System.Windows.Forms.Timer();
            m_dumpTimer.Interval = Constants.DumpDelayInMs;
            m_dumpTimer.Tick += (s, e) => { Dump(); };

            m_filename = filename;

            Enabled = true;
        }

        bool Enabled
        {
            get
            {
                return m_living.Enabled;
            }
            set
            {
                m_dumpTimer.Enabled = value;
                m_living.Enabled = value;
                m_mouse.Enabled = value;
                m_keyboard.Enabled = value;
            }
        }

        private void ForceExit()
        {
            Enabled = false;
            Dump();
        }

        private void Dump()
        {
            Console.WriteLine("Writing " + m_activityList.Count + " activities");
            lock (locker)
            {
                try
                {
                    using (var writer = new StreamWriter(new FileStream(m_filename, FileMode.Append)))
                    {
                        if (WriteText(writer))
                        {
                            m_activityList.Clear();
                        }
                    }
                }
                catch (IOException e)
                {
                    // Most likely to be the output file already in use
                    // Just keep storing Activities until we can access the file
                }
            }
        }

        private Timestamp m_previous;
        private bool WriteText(TextWriter writer)
        {
            Timestamp previous = m_previous;
            using (var text = new StringWriter())
            {
                try
                {
                    m_activityList.ForEach((a) =>
                    {
                        text.Write(a.Timestamp - previous);
                        text.Write(" ");
                        text.Write(a.Type.Id);
                        text.Write(" ");
                        text.Write(a.Info.ToString());
                        text.WriteLine();

                        previous = a.Timestamp;
                    });
                }
                catch (IOException e)
                {
                    return false;
                }

                writer.Write(text.ToString());
                m_previous = previous;
                return true;
            }
        }

        System.Windows.Forms.Timer m_dumpTimer;
        string m_filename;

        MouseLogger m_mouse;
        KeyboardLogger m_keyboard;
        LivingLogger m_living;

        List<Activity> m_activityList;

        private object locker = new object();
    }
}
