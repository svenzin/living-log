using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public class Constants
    {
        public static long TicksPerMs = TimeSpan.TicksPerMillisecond;

        public static int DumpDelayInMs = 60 * 1000; // 1 minute

        public static int SyncDelayInMs = 60 * 60 * 1000; // 1 hour
        public static string SyncFormat = "yyyy-MM-dd_HH:mm:ss.fff";

        public static string LogFilename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\living-log.log";
    }
}
