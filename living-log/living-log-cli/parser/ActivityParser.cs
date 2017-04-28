using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli.parser
{
    public delegate bool DataParser(string s, out IData result);

    public class ActivityParser
    {
        static bool NullParser(string s, out IData result) { result = null; return false; }

        static Dictionary<Category, DataParser> parsers
            = new Dictionary<Category, DataParser>();

        public static void SetParser(Category c, DataParser p)
        {
            parsers[c] = p;
        }

        public static DataParser GetParser(Category c)
        {
            DataParser parser;
            if (!parsers.TryGetValue(c, out parser)) parser = NullParser;
            return parser;
        }

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
}
