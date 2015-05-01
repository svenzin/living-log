using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public struct Timestamp
    {
        public long Milliseconds;

        public Timestamp(DateTime time) : this(time.Ticks / Constants.TicksPerMs) { }

        public DateTime ToDateTime() { return new DateTime(Milliseconds * Constants.TicksPerMs); }

        public override string ToString() { return Milliseconds.ToString(); }

        public static Timestamp operator +(Timestamp a, Timestamp b) { return new Timestamp(a.Milliseconds + b.Milliseconds); }
        public static Timestamp operator -(Timestamp a, Timestamp b) { return new Timestamp(a.Milliseconds - b.Milliseconds); }

        private Timestamp(long milliseconds) { Milliseconds = milliseconds; }
    }
}
