using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public class Activity : IEquatable<Activity>
    {
        public Timestamp Timestamp;
        public Category Type;
        public IData Info;

        public bool Equals(Activity other)
        {
            return (Timestamp == other.Timestamp)
                && (Type == other.Type)
                && (Info.CompareTo(other.Info) == 0);
        }
    }

    public abstract class IData : IComparable<IData>
    {
        public int CompareTo(IData other)
        {
            return ToString().CompareTo(other.ToString());
        }
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString() { return Id.ToString() + " " + Name; }
    }
}
