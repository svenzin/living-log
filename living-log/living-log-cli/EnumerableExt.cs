using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public static class EnumerableExt
    {
        static IList<TSource> ReadBlock<TSource>(IEnumerator<TSource> e, int count)
        {
            var block = new List<TSource>(count);
            while ((count > 0) && e.MoveNext())
            {
                --count;
                block.Add(e.Current);
            }
            return block;
        }

        public static IEnumerable<IList<TSource>> ReadBlocks<TSource>(this IEnumerable<TSource> source, int blockSize)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (blockSize < 1) throw new ArgumentOutOfRangeException("blockSize");

            IEnumerator<TSource> e = source.GetEnumerator();
            while (true)
            {
                var block = ReadBlock(e, blockSize);
                if (block.Count > 0) yield return block;
                else yield break;
            }
        }

        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action f)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (f == null) throw new ArgumentNullException("f");

            var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                f();
                yield return e.Current;
            }
        }
    }
}
