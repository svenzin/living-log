using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace living_log_cli
{
    public class LivingLogger : Logger
    {
        public class SyncData : IData
        {
            public string Version;
            public DateTime Timestamp;
            public override string ToString() { return Timestamp.ToString(Constants.SyncFormat, CultureInfo.InvariantCulture) + " " + Version; }

            public static bool TryParse(string s, out IData result)
            {
                result = null;

                if (string.IsNullOrEmpty(s)) return false;

                var items = s.Split(' ');
                if (items.Length != 2) return false;

                DateTime syncTime;
                if (!DateTime.TryParseExact(items[0], Constants.SyncFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out syncTime)) return false;

                result = new SyncData() { Timestamp = syncTime, Version = items[1] };
                return true;
            }

            public static SyncData Now()
            {
                return At(DateTime.UtcNow);
            }
            public static SyncData At(DateTime t)
            {
                return new SyncData() { Timestamp = t, Version = "1" };
            }
        }

        public static Activity GetSync(Timestamp t)
        {
            return new Activity()
            {
                Timestamp = t,
                Type = Categories.LivingLog_Sync,
                Info = SyncData.At(t.ToDateTime())
            };
        }

        public LivingLogger(int syncDelay)
        {
            m_sync = new Timer();
            m_sync.AutoReset = true;
            m_sync.Interval = syncDelay;
            m_sync.Elapsed += (s, e) => { Invoke(Categories.LivingLog_Sync, SyncData.Now()); };
            m_sync.Enabled = false;
        }

        public override bool Enabled
        {
            get
            {
                return m_sync.Enabled;
            }
            set
            {
                var enable = value;
                if (enable != m_sync.Enabled)
                {
                    m_sync.Enabled = enable;
                    if (enable)
                    {
                        Invoke(Categories.LivingLog_Startup, SyncData.Now());
                    }
                    else
                    {
                        Invoke(Categories.LivingLog_Exit, SyncData.Now());
                    }
                }
            }
        }

        private Timer m_sync;
    }
}
