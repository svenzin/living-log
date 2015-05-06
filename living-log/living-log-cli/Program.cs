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
    class LivingFile
    {
        public static bool Exists(string filename)
        {
            return File.Exists(filename);
        }

        public string Filename { get; set; }

        public struct Stats
        {
            public long Length { get; internal set; }
            public long Count { get; internal set; }
        }
        public static Stats GetStats(string filename)
        {
            return new Stats()
            {
                Length = new FileInfo(filename).Length,
                Count = File.ReadLines(filename).LongCount()
            };
        }

        public struct Info
        {
            public string Name { get; internal set; }
            
            public string BaseName { get; internal set; }
            public string Extension { get; internal set; }

            public string Child(int year, int month)
            {
                return BaseName
                    + "."
                    + year.ToString().PadLeft(4, '0')
                    + "-"
                    + month.ToString().PadLeft(2, '0')
                    + Extension;
            }
        }
        public static Info GetInfo(string filename)
        {
            var i = filename.LastIndexOf('.');
            return new Info()
            {
                Name = filename,
                BaseName = (i >= 0) ? filename.Substring(0, i) : filename,
                Extension = (i >= 0) ? filename.Substring(i) : String.Empty,
            };
        }

        public static IEnumerable<Activity> ReadActivities(string filename)
        {
            var t = new Timestamp();
            return File.ReadLines(filename)
                .Select((s) =>
                {
                    Activity act = null;
                    if (Program.TryParseActivity(s, out act))
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
                .WhereValid();
        }
    }

    public static class ActivityTools
    {
        // A valid activity list
        // - is non-null
        // - has non-null items
        // - starts with a sync information
        public static bool IsValid(IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            if (source.Any())
            {
                var item = source.First();
                if (item == null) return false;
                if (!Categories.IsSync(item.Type)) return false;

                var t0 = item.Timestamp;
                foreach (var a in source.Skip(1))
                {
                    if (a == null) return false;
                }
            }

            return true;
        }

        public static bool IsChronological(IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            if (source.Any())
            {
                var t0 = source.First().Timestamp;
                foreach (var a in source.Skip(1))
                {
                    var t = t0;
                    t0 = a.Timestamp;
                    if (t0 < t) return false;
                }
            }

            return true;
        }

        public static IEnumerable<Activity> WhereValid(this IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source
                .Where(a => a != null)
                .SkipWhile(a => !Categories.IsSync(a.Type));
        }

        public static IEnumerable<Activity> WhereChronological(this IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.IsEmpty()) return source;
            var t0 = source.First().Timestamp;
            return source
                .Where(a => a != null)
                .TakeWhile(a =>
                {
                    var t = t0;
                    t0 = a.Timestamp;
                    return t <= t0;
                });
        }

        public static IEnumerable<Activity> MakeValid(this IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            // Non-null
            var activities = source.Where((a) => a != null);

            // Synced
            if (activities.Any())
            {
                var item = activities.First();
                if (!Categories.IsSync(item.Type))
                {
                    activities = new List<Activity>() { LivingLogger.GetSync(item.Timestamp) }.Concat(activities);
                }
            }

            return activities.ToList();
        }

        static IEnumerable<Activity> MergeIterator(IEnumerator<Activity> a, IEnumerator<Activity> b)
        {
            bool hasA = a.MoveNext();
            bool hasB = b.MoveNext();

            while (hasB)
            {
                var t = b.Current.Timestamp;
                while (hasA)
                {
                    if (a.Current.Timestamp <= t)
                    {
                        yield return a.Current;
                        hasA = a.MoveNext();
                    }
                    else break;
                }

                var tmp = a; a = b; b = tmp;
                var hasTmp = hasA; hasA = hasB; hasB = hasTmp;
            }

            while (hasA)
            {
                yield return a.Current;
                hasA = a.MoveNext();
            }
        }
        public static IEnumerable<Activity> Merge(IEnumerable<Activity> sourceA, IEnumerable<Activity> sourceB)
        {
            if (sourceA == null) throw new ArgumentNullException("sourceA");
            if (sourceB == null) throw new ArgumentNullException("sourceB");

            if (sourceA.IsEmpty()) return sourceB;
            if (sourceB.IsEmpty()) return sourceA;

            return MergeIterator(sourceA.GetEnumerator(), sourceB.GetEnumerator());
        }

        public static IEnumerable<Activity> Merge(IEnumerable<IEnumerable<Activity>> sources)
        {
            if (sources == null) throw new ArgumentNullException("sources");
            if (sources.IsEmpty()) return Enumerable.Empty<Activity>();

            var queue = new Queue<IEnumerable<Activity>>();
            foreach (var s in sources) queue.Enqueue(s);

            while (queue.Count > 1)
            {
                var a = queue.Dequeue();
                var b = queue.Dequeue();
                queue.Enqueue(Merge(a, b));
            }
            return queue.Dequeue();
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
        
        void Pause()
        {
            if (Enabled)
            {
                Enabled = false;
                Output.WriteLine("Paused");
            }
        }

        void Resume()
        {
            if (!Enabled)
            {
                Enabled = true;
                Output.WriteLine("Resumed");
            }
        }

        void FileStats()
        {
            if (LivingFile.Exists(m_filename))
            {
                var stats = LivingFile.GetStats(m_filename);
                Output.WriteLine("File " + m_filename + " has " + ToHumanString(stats.Length) + " bytes");
                Output.WriteLine("File " + m_filename + " has " + ToHumanString(stats.Count) + " lines");
            }
        }

        void FileSplit()
        {
            if (Enabled)
            {
                Output.WriteLine("Cannot split while logging. Please pause.");
            }
            else
            {
                if (LivingFile.Exists(m_filename))
                {
                    Dump();

                    var info = LivingFile.GetInfo(m_filename);
                    Output.WriteLine("Files of names " + info.BaseName + ".YYYY-MM" + info.Extension + " will be generated");

                    System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();

                    long counter = 0;
                    var logger = new System.Timers.Timer();
                    logger.AutoReset = true;
                    logger.Interval = Constants.Second;
                    logger.Elapsed += (s, e) => { Header("activities: " + ToHumanString(counter).PadRight(8) + " elapsed: " + w.Elapsed.ToString()); };
                    logger.Start();

                    var backups = new Dictionary<string, string>();

                    try
                    {
                        w.Start();
                        var activities = LivingFile.ReadActivities(m_filename)
                            .Do(() => ++counter);

                        foreach (var activityBlock in activities.ReadBlocks(Constants.ReadingBlockSize))
                        {
                            var groups = activityBlock
                                .GroupBy((a) =>
                                {
                                    var at = a.Timestamp.ToDateTime();
                                    return new { Year = at.Year, Month = at.Month };
                                });

                            foreach (var group in groups)
                            {
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
                                        var filename = info.Child(group.Key.Year, group.Key.Month);

                                        var backup = filename + ".bak";
                                        if (!backups.ContainsKey(filename))
                                        {
                                            if (File.Exists(filename))
                                            {
                                                if (File.Exists(backup)) File.Delete(backup);
                                                File.Copy(filename, backup);
                                            }
                                            backups.Add(filename, backup);
                                            Output.WriteLine("Writing to " + filename);
                                        }

                                        Timestamp previous = groupActivities.First().Timestamp;
                                        using (var writer = new StreamWriter(File.Open(filename, FileMode.Append)))
                                        {
                                            foreach (var groupActivityBlock in groupActivities.ReadBlocks(Constants.WritingBlockSize))
                                            {
                                                WriteText(groupActivityBlock, ref previous, writer);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Header("Split task successful.");
                        foreach (var kvp in backups)
                        {
                            if (File.Exists(kvp.Value)) File.Delete(kvp.Value);
                        }
                        File.Create(m_filename);
                    }
                    catch
                    {
                        Header("Error during split task. Removing temporary files...");
                        foreach (var kvp in backups)
                        {
                            if (File.Exists(kvp.Key)) File.Delete(kvp.Key);
                            if (File.Exists(kvp.Value)) File.Move(kvp.Value, kvp.Key);
                        }
                    }
                    finally
                    {
                        logger.Stop();
                    }
                }
            }
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
                    Pause();
                }
                else if (command.Equals("resume") || command.Equals("r"))
                {
                    Resume();
                }
                else if (command.Equals("stats"))
                {
                    FileStats();
                }
                else if (command.Equals("split"))
                {
                    FileSplit();
                }
            } while (!command.Equals("exit"));

            Program.ForceExit();
        }

        public static bool TryParseActivity(string s, out Activity result)
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
                    Timestamp previous = m_previous;
                    using (var writer = new StreamWriter(File.Open(m_filename, FileMode.Append)))
                    {
                        // Only change m_previous is writing is successful
                        WriteText(m_activityList, ref previous, writer);
                    }
                    m_previous = previous;
                    m_activityList.Clear();
                }
                catch (IOException e)
                {
                    // Most likely to be the output file already in use
                    // Just keep storing Activities until we can access the file
                }
            }
        }
        
        void WriteText(IList<Activity> activities, ref Timestamp previous, TextWriter writer)
        {
            using (var text = new StringWriter())
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
                
                writer.Write(text.ToString());
            }
        }
        
        private Timestamp m_previous;

        System.Timers.Timer m_dumpTimer;
        string m_filename;

        MouseLogger m_mouse;
        KeyboardLogger m_keyboard;
        LivingLogger m_living;

        List<Activity> m_activityList;

        private object locker = new object();
    }
}
