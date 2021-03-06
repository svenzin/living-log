﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Diagnostics;

namespace living_log_cli
{
    public static class ActivityTools
    {
        public static IEnumerable<Activity> Process(IEnumerable<Activity> source)
        {
            //var w = new Stopwatch();
            //w.Start();

            //var t0 = w.Elapsed;
            //var list = source.ToList();
            //var tList = w.Elapsed;
            //var valid = WhereValid(list).ToList();
            //var tValid = w.Elapsed;
            //var parts = PartitionChronological(valid).ToList();
            //var tParts = w.Elapsed;
            //var merge = Merge(parts).ToList();
            //var tMerge = w.Elapsed;
            //var dupe = RemoveDuplicates(merge).ToList();
            //var tDupe = w.Elapsed;
            //w.Stop();
            //return dupe;
            return RemoveDuplicates(
                Merge(
                PartitionChronological(
                WhereValid(
                    source
                    ))));
        }

        // A valid activity list
        // - is non-null
        // - has non-null items
        // - starts with a sync information
        public static bool IsValid(ICollection<Activity> source)
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

        public static bool IsChronological(ICollection<Activity> source)
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

                Tools.Swap(ref a, ref b);
                Tools.Swap(ref hasA, ref hasB);
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

            var queue = new Queue<IEnumerable<Activity>>(sources);

            while (queue.Count > 1)
            {
                var a = queue.Dequeue();
                var b = queue.Dequeue();
                queue.Enqueue(Merge(a, b));
            }
            return queue.Dequeue();
        }

        static IEnumerable<Activity> DuplicateIterator(this IEnumerable<Activity> source)
        {
            var items = new Queue<Activity>();

            var t = source.First().Timestamp;

            foreach (var a in source)
            {
                if (a.Timestamp != t)
                {
                    while (items.Count > 0) yield return items.Dequeue();
                    t = a.Timestamp;
                }

                if (!items.Contains(a)) items.Enqueue(a);
            }
            while (items.Count > 0) yield return items.Dequeue();
        }
        public static IEnumerable<Activity> RemoveDuplicates(IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.IsEmpty()) return source;

            return DuplicateIterator(source);
        }

        public static IEnumerable<IList<Activity>> PartitionChronological(IEnumerable<Activity> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.IsEmpty()) yield break;

            var t = source.First().Timestamp;
            var items = new List<Activity>();
            foreach (var a in source)
            {
                if (a.Timestamp < t) { yield return items; items = new List<Activity>(); }
                items.Add(a);
                t = a.Timestamp;
            }
            if (items.Count > 0) yield return items;
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

        private static UI ui;
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
            ui = new UI();
            new Thread(() => { p.REPL(ui); }) { IsBackground = true }.Start();
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

        void Pause()
        {
            if (Enabled)
            {
                Enabled = false;
                Ui.WriteLine("Paused");
            }
        }

        void Resume()
        {
            if (!Enabled)
            {
                Enabled = true;
                Ui.WriteLine("Resumed");
            }
        }

        void FileStats()
        {
            if (LivingFile.Exists(m_filename))
            {
                var stats = LivingFile.GetStats(m_filename);
                Ui.WriteLine("File " + m_filename + " has " + Tools.ToHumanString(stats.Length) + " bytes");
                Ui.WriteLine("File " + m_filename + " has " + Tools.ToHumanString(stats.Count) + " lines");
            }
        }

        void FileSplit()
        {
            if (Enabled)
            {
                Ui.WriteLine("Cannot split while logging. Please pause.");
            }
            else
            {
                if (LivingFile.Exists(m_filename))
                {
                    Dump();

                    var info = LivingFile.GetInfo(m_filename);
                    Ui.WriteLine("Files of names " + info.BaseName + ".YYYY-MM" + info.Extension + " will be generated");

                    System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();

                    long counter = 0;
                    var logger = new System.Timers.Timer();
                    logger.AutoReset = true;
                    logger.Interval = Constants.Second;
                    logger.Elapsed += (s, e) => { Ui.SetStatus("activities: " + Tools.ToHumanString(counter).PadRight(8) + " elapsed: " + w.Elapsed.ToString()); };
                    logger.Start();

                    var backups = new Dictionary<string, string>();

                    try
                    {
                        w.Start();
                        var activities = LivingFile.ReadActivities(m_filename)
                            .Do(() => ++counter);

                        foreach (var activityBlock in activities.PartitionBlocks(Constants.ReadingBlockSize))
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
                                        groupActivities = Enumerable.Repeat(LivingLogger.GetSync(item.Timestamp), 1).Concat(group);
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
                                            Ui.WriteLine("Writing to " + filename);
                                        }

                                        Timestamp previous = groupActivities.First().Timestamp;
                                        using (var writer = new StreamWriter(File.Open(filename, FileMode.Append)))
                                        {
                                            foreach (var groupActivityBlock in groupActivities.PartitionBlocks(Constants.WritingBlockSize))
                                            {
                                                WriteText(groupActivityBlock, ref previous, writer);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Ui.SetStatus("Split task successful.");
                        foreach (var kvp in backups)
                        {
                            counter = 0;
                            w.Restart();
                            Ui.WriteLine("Processing " + kvp.Key);

                            if (File.Exists(kvp.Value)) File.Delete(kvp.Value);
                            if (File.Exists(kvp.Key)) File.Copy(kvp.Key, kvp.Value);

                            var processed =
                                ActivityTools.Process(
                                    LivingFile
                                    .ReadActivities(kvp.Value)
                                    .Do(() => ++counter)
                                );
                            Timestamp previous = processed.First().Timestamp;
                            using (var writer = new StreamWriter(File.Create(kvp.Key)))
                            {
                                foreach (var pBlock in processed.PartitionBlocks(Constants.WritingBlockSize))
                                {
                                    WriteText(pBlock, ref previous, writer);
                                }
                            }
                        }
                        foreach (var kvp in backups)
                        {
                            if (File.Exists(kvp.Value)) File.Delete(kvp.Value);
                        }
                        using (var file = File.Create(m_filename))
                        {
                            // "using" makes sura that the file is properly closed and not still in use
                        }
                        Ui.SetStatus("Processing task successful.");
                    }
                    catch (Exception e)
                    {
                        Ui.SetStatus("Error during split task. Removing temporary files...");
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

        class UI
        {
            private string Status;
            public long Count;
            public long Total;

            public UI()
            {
                Status = string.Empty;
                Count = 0;
                Total = 0;
                Update();
            }

            private void Update()
            {
                var x = Console.CursorLeft;
                var y = Console.CursorTop;
                string blank = new string(' ', Console.WindowWidth - 1);
                string separator = new string('_', Console.WindowWidth - 1);
                string status = Status.PadRight(Console.WindowWidth - 1);
                string info = (Constants.LogFilename + " " + Tools.ToHumanString(Count).PadLeft(6) + " | " + Tools.ToHumanString(Total).PadLeft(6)).PadRight(Console.WindowWidth - 1);
                Console.SetCursorPosition(Console.WindowLeft, Console.WindowTop);
                Console.WriteLine(info);
                Console.WriteLine(status);
                Console.WriteLine(separator);
                Console.WriteLine(blank);
                Console.SetCursorPosition(x, y);
            }

            public string ReadLine()
            {
                var s = Console.ReadLine();
                Update();
                return s;
            }

            public void Write(string s) { Console.Write(s); }
            public void WriteLine(string s)
            {
                Console.WriteLine(s);
                Update();
            }
            
            public void SetStatus(string s)
            {
                Status = s;
                Update();
            }
        }
        private UI Ui;
        void REPL(UI ui)
        {
            Ui = ui;

            string command = string.Empty;
            do
            {
                ui.Write("ll > ");
                command = (ui.ReadLine() ?? "exit").Trim();
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
                else if (command.Equals("test"))
                {
                    var activities = LivingFile.ReadActivities("C:\\Users\\scorder\\Documents\\living-log.2015-04.log");
                    ActivityTools.Process(activities);
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
            Ui.SetStatus("Logging " + m_activityList.Count + " activities...");

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
                    ui.Count = m_activityList.Count;
                    ui.Total += ui.Count;
                    m_activityList.Clear();

                    Ui.SetStatus("Logged " + m_activityList.Count + " activities");
                }
                catch (IOException e)
                {
                    Ui.SetStatus("Could not write to living log");
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
