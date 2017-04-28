using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli.parser
{
    public class LivingFile
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
                    if (ActivityParser.TryParse(s, out act))
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
                .Where(a => a != null);
        }
    }
}
