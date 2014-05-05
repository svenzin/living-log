using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public class Logger
    {
        public delegate void ActivityLoggedHandler(object sender, Activity activity);
        public event ActivityLoggedHandler ActivityLogged;

        protected void Invoke(Category category, IData data)
        {
            if (ActivityLogged != null) ActivityLogged(this, new Activity() { Timestamp = new Timestamp(DateTime.UtcNow), Type = category, Info = data });
        }
    }
}
