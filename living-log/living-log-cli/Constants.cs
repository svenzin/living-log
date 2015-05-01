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

        public static int Millisecond = 1;
        public static int Second = 1000;
        public static int Minute = 60000;
        public static int Hour = 3600000;

        public static int DumpDelayInMs = Minute;

        public static int SyncDelayInMs = Hour;
        public static string SyncFormat = "yyyy-MM-dd_HH:mm:ss.fff";

        public static string LogFilename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\living-log.log";

        public static int ReadingBlockSize = 1000000;
        public static int WritingBlockSize = 1000;
    }
}
