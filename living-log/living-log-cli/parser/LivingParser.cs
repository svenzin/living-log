using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli.parser
{
    class LivingParser
    {
        public static bool TryParse(string s, out IData result)
        {
            result = null;

            if (string.IsNullOrEmpty(s)) return false;

            var items = s.Split(' ');
            if (items.Length != 2) return false;

            DateTime syncTime;
            if (!DateTime.TryParseExact(items[0], Constants.SyncFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out syncTime)) return false;

            result = new LivingLogger.SyncData() { Timestamp = syncTime, Version = items[1] };
            return true;
        }
    }
}
