using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace living_log_cli
{
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
                        Invoke(Categories.LivingLog_Startup, new SyncData() { Timestamp = DateTime.UtcNow, Version = "1" });
                    }
                    else
                    {
                        Invoke(Categories.LivingLog_Exit, new SyncData() { Timestamp = DateTime.UtcNow, Version = "1" });
                    }
                }
            }
        }

        private Timer m_sync;
    }
}
