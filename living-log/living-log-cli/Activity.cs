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

        static Dictionary<Category, IData.TryParser> parsers
            = new Dictionary<Category, IData.TryParser>();
        public static void SetParser(Category c, IData.TryParser p)
        {
            parsers[c] = p;
        }
        public static IData.TryParser GetParser(Category c)
        {
            IData.TryParser parser;
            if (!parsers.TryGetValue(c, out parser)) parser = NullParser;
            return parser;
        }
        static bool NullParser(string s, out IData result) { result = null; return false; }

        public static bool TryParse(string s, out Activity result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var items = s.Split(new char[] { ' ' }, 3);
            if (items.Length != 3) return false;

            var dT = new Timestamp();
            if (!long.TryParse(items[0], out dT.Milliseconds)) return false;

            int type;
            if (!int.TryParse(items[1], out type)) return false;

            Category cat = Categories.get(type);
            if (cat == null) return false;

            IData data;
            if (!GetParser(cat)(items[2], out data)) return false;

            result = new Activity() { Timestamp = dT, Type = cat, Info = data };
            return true;
        }
    }

    public abstract class IData : IComparable<IData>
    {
        public delegate bool TryParser(string s, out IData result);

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

        public static readonly Category Unknown = new Category() { Id = -1, Name = "Unknown" };
    }
}
