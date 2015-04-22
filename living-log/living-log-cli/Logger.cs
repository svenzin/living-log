using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public abstract class Logger
    {
        public delegate void ActivityLoggedHandler(object sender, Activity activity);
        public event ActivityLoggedHandler ActivityLogged;

        private bool m_enabled = false;
        public bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (m_enabled != value)
                {
                    m_enabled = value;
                    if (!Enabled) Enable(); else Disable();
                }
            }
        }

        protected abstract void Enable();
        protected abstract void Disable();
        protected void Invoke(Category category, IData data)
        {
            if (ActivityLogged != null) ActivityLogged(this, new Activity() { Timestamp = new Timestamp(DateTime.UtcNow), Type = category, Info = data });
        }
    }
}
