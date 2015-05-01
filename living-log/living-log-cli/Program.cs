using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Globalization;

namespace living_log_cli
{
    public static class EnumerableExt
    {
        static IList<TSource> ReadBlock<TSource>(IEnumerator<TSource> e, int count)
        {
            var block = new List<TSource>(count);
            while ((count > 0) && e.MoveNext())
            {
                --count;
                block.Add(e.Current);
            }
            return block;
        }

        public static IEnumerable<IList<TSource>> ReadBlocks<TSource>(this IEnumerable<TSource> source, int blockSize)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (blockSize < 1) throw new ArgumentOutOfRangeException("blockSize");

            IEnumerator<TSource> e = source.GetEnumerator();
            while (true)
            {
                var block = ReadBlock(e, blockSize);
                if (block.Count > 0) yield return block;
                else yield break;
            }
        }

        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action f)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (f == null) throw new ArgumentNullException("f");

            var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                f();
                yield return e.Current;
            }
        }
    }

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

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            Program p = new Program(Constants.LogFilename);
            SetExitHandler((t) =>
            {
                ForceExit();
                return true;
            });

            Console.SetCursorPosition(0, 4);
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

        private void Header(string message)
        {
            var pos = new { x = Console.CursorLeft, y = Console.CursorTop };
            Console.SetCursorPosition(Console.WindowLeft, Console.WindowTop);
            Console.WriteLine(("Using " + Constants.LogFilename + " as log file").PadRight(Console.BufferWidth - 1));
            Console.WriteLine(message.PadRight(Console.BufferWidth - 1));
            Console.WriteLine(string.Empty.PadRight(Console.BufferWidth - 1));
            Console.SetCursorPosition(pos.x, pos.y);
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
                Header(string.Empty);
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
                else if (command.Equals("stats"))
                {
                    if (File.Exists(m_filename))
                    {
                        var info = new FileInfo(m_filename);
                        Output.WriteLine("File " + info.Name + " has " + ToHumanString(info.Length) + " bytes");

                        var lines = File.ReadLines(m_filename);
                        Output.WriteLine("File " + info.Name + " has " + ToHumanString(lines.Count()) + " lines");
                    }
                }
                else if (command.Equals("split"))
                {
                    if (Enabled)
                    {
                        Output.WriteLine("Cannot split while logging. Please pause.");
                    }
                    else
                    {
                        if (File.Exists(m_filename))
                        {
                            var i = m_filename.LastIndexOf('.');
                            var name = (i >= 0) ? m_filename.Substring(0, i) : m_filename;
                            var ext = (i >= 0) ? m_filename.Substring(i) : String.Empty;
                            Output.WriteLine("Files of names " + name + ".YYYY-MM" + ext + " will be generated");

                            Timestamp t = new Timestamp(DateTime.MinValue);
                            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();

                            long counter = 0;
                            var logger = new System.Timers.Timer();
                            logger.AutoReset = true;
                            logger.Interval = 1000;
                            logger.Elapsed += (s, e) => { Header("activities: " + ToHumanString(counter).PadRight(8) + " elapsed: " + w.Elapsed.ToString()); };
                            logger.Enabled = true;

                            w.Start();
                            var activities = File.ReadLines(m_filename)
                                .Select((s) =>
                                {
                                    Activity act = null;
                                    if (TryParseActivity(s, out act))
                                    {
                                        if (Categories.IsSync(act.Type))
                                        {
                                            var t0 = (act.Info as LivingLogger.SyncData).Timestamp;
                                            t = new Timestamp(t0);

                                            act.Timestamp = t;
                                        }
                                        else
                                        {
                                            t = t + act.Timestamp;
                                            act.Timestamp = t;
                                        }
                                    }
                                    return act;
                                })
                                .Where((a) => a != null)
                                .SkipWhile((a) =>
                                {
                                    return !Categories.IsSync(a.Type);
                                })
                                .Do(() => ++counter);

                            foreach (var activityBlock in activities.ReadBlocks(10000000))
                            {
                                var groups = activityBlock
                                    .GroupBy((a) =>
                                    {
                                        var at = a.Timestamp.ToDateTime();
                                        return new { Year = at.Year, Month = at.Month };
                                    });

                                foreach (var group in groups)
                                {
                                    //Output.WriteLine(group.Key.Year.ToString().PadLeft(4, '0') + "-" + group.Key.Month.ToString().PadLeft(2, '0') + " has " + group.Count() + " activities");

                                    if (group.Any())
                                    {
                                        IEnumerable<Activity> groupActivities;
                                        var item = group.First();
                                        if (!Categories.IsSync(item.Type))
                                        {
                                            // When appending activities, always start with a sync activity
                                            // This will help "resorting" activities if needed
                                            groupActivities = new List<Activity>()
                                            {
                                                LivingLogger.GetSync(item.Timestamp)
                                            }.Concat(group);
                                        }
                                        else
                                        {
                                            groupActivities = group;
                                        }

                                        lock (locker)
                                        {
                                            var filename = name + "." + group.Key.Year.ToString() + "-" + group.Key.Month.ToString().PadLeft(2, '0') + ext;
                                            using (var writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write)))
                                            {
                                                Output.WriteLine("Writing to " + filename);

                                                Timestamp previous = groupActivities.First().Timestamp;
                                                try
                                                {
                                                    foreach (var groupActivityBlock in groupActivities.ReadBlocks(1000))
                                                    {
                                                        using (var text = new StringWriter())
                                                        {
                                                            WriteText(groupActivityBlock, ref previous, text);
                                                            writer.Write(text.ToString());
                                                        }
                                                    }
                                                }
                                                catch (IOException e)
                                                {
                                                    throw e;
                                                    // Most likely to be the output file already in use
                                                    // Just keep storing Activities until we can access the file
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } while (!command.Equals("exit"));

            Program.ForceExit();
        }

        public bool TryParseActivity(string s, out Activity result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var items = s.Split(new char[] { ' ' }, 3);
            if (items.Length != 3) return false;

            var dT = new Timestamp();
            if (!long.TryParse(items[0], out dT.Milliseconds)) return false;

            int type;
            if (!int.TryParse(items[1], out type)) return false;

            Category cat = Categories.get(type);
            if (cat == null) return false;
            
            IData data;
            if (!Categories.parser(cat)(items[2], out data)) return false;
           
            result = new Activity() { Timestamp = dT, Type = cat, Info = data };
            return true;
        }


        public static string ToHumanString(long value)
        {
            string[] units = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
            int i = 0;
            while (value > 2500)
            {
                value = value / 1000;
                ++i;
            }
            return value.ToString() + units[i];
        }
        
        Program(string filename)
        {
            m_filename = filename;

            m_activityList = new List<Activity>();

            m_previous = new Timestamp(DateTime.UtcNow);

            m_living = new LivingLogger(Constants.SyncDelayInMs);
            m_living.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            //m_mouse = new MouseLogger();
            //m_mouse.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

            //m_keyboard = new KeyboardLogger();
            //m_keyboard.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };

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
                //m_mouse.Enabled = value;
                //m_keyboard.Enabled = value;
            }
        }

        private void Dump()
        {
            Header("Writing " + m_activityList.Count + " activities");

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
        
        void WriteText(IList<Activity> activities, ref Timestamp previous, StringWriter writer)
        {
            foreach (var a in activities)
            {
                writer.Write(a.Timestamp - previous);
                writer.Write(" ");
                writer.Write(a.Type.Id);
                writer.Write(" ");
                writer.Write(a.Info.ToString());
                writer.WriteLine();

                previous = a.Timestamp;
            };
        }
        
        private Timestamp m_previous;
        private bool WriteText(TextWriter writer)
        {
            Timestamp previous = m_previous;
            using (var text = new StringWriter())
            {
                try
                {
                    WriteText(m_activityList, ref previous, text);
                }
                catch (IOException)
                {
                    return false;
                }

                writer.Write(text.ToString());
                m_previous = previous;
                return true;
            }
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
