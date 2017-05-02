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
        public static Activity GetSync(Timestamp t)
        {
            return new Activity()
            {
                Timestamp = t,
                Type = Categories.LivingLog_Sync,
                Info = SyncData.At(t.ToDateTime())
            };
        }

        public class SyncData : IData
        {
            public string Version;
            public DateTime Timestamp;
            public override string ToString() { return Timestamp.ToString(Constants.SyncFormat) + " " + Version; }

            public static SyncData At(DateTime t)
            {
                return new SyncData() { Timestamp = t, Version = "1" };
            }
            public static SyncData Now() { return At(DateTime.UtcNow); }
        }

        public LivingLogger(int syncDelay)
        {
            m_sync = new Timer();
            m_sync.Interval = syncDelay;
            m_sync.Tick += (s, e) => { Invoke(Categories.LivingLog_Sync, SyncData.Now()); };
            m_sync.Enabled = this.Enabled;
        }

        protected override void Enable()
        {
            m_sync.Enabled = true;
            Invoke(Categories.LivingLog_Startup, SyncData.Now());
        }
        
        protected override void Disable()
        {
            m_sync.Enabled = false;
            Invoke(Categories.LivingLog_Exit, SyncData.Now());
        }

        private Timer m_sync;
    }
}
