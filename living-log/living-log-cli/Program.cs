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

            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);

            Console.WriteLine("Using " + Constants.LogFilename + " as log file");
            Console.WriteLine();
            Console.WriteLine();

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            Program p = new Program(Constants.LogFilename);
            SetExitHandler((t) =>
            {
                ForceExit();
                return true;
            });

            new Thread(() => { p.REPL(Console.In, Console.Out); }) { IsBackground = true }.Start();
            System.Windows.Forms.Application.Run();

            //new Thread(() => { System.Windows.Forms.Application.Run(); }) { IsBackground = true }.Start();
            //p.REPL(Console.In, Console.Out);

            p.Enabled = false;
            p.Dump();
        }
        static void ForceExit()
        {
            System.Windows.Forms.Application.Exit();
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
                Output.Write("ll > ");
                command = (Input.ReadLine() ?? "exit").Trim();
                if (command.Equals("pause") || command.Equals("p"))
                {
                    if (Enabled)
                    {
                        Enabled = false;
                        Output.WriteLine("Paused");
                    }
                }
                else if (command.Equals("resume") || command.Equals("r"))
                {
                    if (!Enabled)
                    {
                        Enabled = true;
                        Output.WriteLine("Resumed");
                    }
                }
                else if (command.Equals("split"))
                {
                    Split();
                }
                else if (command.Equals("stat") || command.Equals("stats"))
                {
                    Stat();
                }
                else if (command.Equals("dump"))
                {
                    Dump();
                }
            } while (!command.Equals("exit"));

            Program.ForceExit();
        }
        
        Program(string filename)
        {
            m_filename = filename;

            m_activityList = new List<Activity>();

            m_previous = new Timestamp(DateTime.UtcNow);

            m_living = new LivingLogger(Constants.SyncDelayInMs);
            m_living.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_mouse = new MouseLogger();
            m_mouse.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_keyboard = new KeyboardLogger();
            m_keyboard.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            m_dumpTimer = new System.Timers.Timer()
            {
                Interval = Constants.DumpDelayInMs,
                AutoReset = true,
            };
            m_dumpTimer.Elapsed += (s, e) => Dump();

            Enabled = true;
        }

        bool Enabled
        {
            get
            {
                return m_dumpTimer.Enabled;
            }
            set
            {
                m_dumpTimer.Enabled = value;
                m_living.Enabled = value;
                m_mouse.Enabled = value;
                m_keyboard.Enabled = value;
            }
        }

        private void Dump()
        {
            var pos = new { x = Console.CursorLeft, y = Console.CursorTop };
            string blank = new string(' ', Console.BufferWidth);
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(blank);
            Console.WriteLine(blank);
            Console.WriteLine(blank);
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Using " + Constants.LogFilename + " as log file");
            Console.WriteLine("Writing " + m_activityList.Count + " activities");
            Console.SetCursorPosition(pos.x, pos.y);

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

        private void Stat()
        {
            var info = parser.LivingFile.GetInfo(m_filename);
            var stats = parser.LivingFile.GetStats(m_filename);

            Console.WriteLine("File " + info.Name);
            Console.WriteLine("    " + stats.Length + " bytes");
            Console.WriteLine("    " + stats.Count + " lines");
        }

        private void Split()
        {
            var today = DateTime.UtcNow.Date;
            if (parser.LivingFile.Exists(m_filename))
            {
                Dump();

                lock (locker)
                {
                    var info = parser.LivingFile.GetInfo(m_filename);
                    var stats = parser.LivingFile.GetStats(m_filename);

                    Console.WriteLine("File " + info.Name);
                    Console.WriteLine("    " + stats.Length + " bytes");
                    Console.WriteLine("    " + stats.Count + " lines");
                    Console.WriteLine("Filename pattern: " + info.BaseName + ".UTC.YYYY-MM-DD" + info.Extension);

                    var date = DateTime.MinValue.Date;

                    var success = true;
                    var files = new List<string>();

                    var buffer = new List<Activity>();
                    var activities = parser.LivingFile.ReadActivities(m_filename);

                    Action WriteBuffer = () =>
                    {
                        if (buffer.Count > 0)
                        {
                            var filename = GetSplitFilename(date, today, info);
                            Console.WriteLine("Writing to file " + filename);
                            success = success && WriteSplitToFile(filename, buffer);
                            Console.WriteLine("Wrote " + buffer.Count + " activities");
                            files.Add(filename);
                            buffer.Clear();
                        }
                    };

                    foreach (var a in activities)
                    {
                        var t = a.Timestamp.ToDateTime();
                        if (t.Date != date)
                        {
                            WriteBuffer();
                            Console.WriteLine("Parsing " + date.ToString("yyyy-MM-dd") + "...");
                            date = t.Date;
                        }
                        buffer.Add(a);
                    }
                    WriteBuffer();

                    if (success)
                    {
                        Console.WriteLine("Split successful.");
                        if (File.Exists(m_filename)) File.Delete(m_filename);
                        var tempfile = GetSplitFilename(today, today, info);
                        if (File.Exists(tempfile)) File.Move(tempfile, m_filename);
                    }
                    else
                    {
                        Console.WriteLine("Split failed. Cleaning up...");
                        foreach (var filename in files)
                        {
                            if (File.Exists(filename))
                            {
                                File.Delete(filename);
                                Console.WriteLine("    Removed " + filename);
                            }
                        }
                    }
                }
            }
        }

        private static string GetSplitFilename(DateTime date, DateTime today, parser.LivingFile.Info info)
        {
            if (date == today)
            {
                return info.BaseName + info.Extension + ".tmp";
            }
            return info.BaseName + ".UTC." + date.ToString("yyyy-MM-dd") + info.Extension;
        }

        private static bool WriteSplitToFile(string filename, IEnumerable<Activity> buffer)
        {
            bool success = true;
            using (var writer = new StreamWriter(File.Open(filename, FileMode.Append)))
            {
                var ts = buffer.First().Timestamp;
                var sync = LivingLogger.GetSync(ts);
                var previous = ts;
                success = success && WriteText(Enumerable.Repeat(sync, 1), ref previous, writer);
                success = success && WriteText(buffer, ref previous, writer);
            }
            return success;
        }

        private static bool WriteText(IEnumerable<Activity> activities, ref Timestamp previous, TextWriter writer)
        {
            using (var text = new StringWriter())
            {
                try
                {
                    foreach (var a in activities)
                    {
                        text.Write(a.Timestamp - previous);
                        text.Write(" ");
                        text.Write(a.Type.Id);
                        text.Write(" ");
                        text.Write(a.Info.ToString());
                        text.WriteLine();

                        previous = a.Timestamp;
                    };
                }
                catch (IOException e)
                {
                    return false;
                }

                writer.Write(text.ToString());
                return true;
            }
        }

        private Timestamp m_previous;
        private bool WriteText(TextWriter writer)
        {
            Timestamp previous = m_previous;
            if (WriteText(m_activityList, ref previous, writer))
            {
                m_previous = previous;
                return true;
            }
            return false;
        }

        System.Timers.Timer m_dumpTimer;
        string m_filename;

        MouseLogger m_mouse;
        KeyboardLogger m_keyboard;
        LivingLogger m_living;

        List<Activity> m_activityList;

        private object locker = new object();
    }
}
