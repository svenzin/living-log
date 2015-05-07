using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public static class EnumerableExt
    {
        public static IEnumerable<IList<TSource>> PartitionBlocks<TSource>(this IEnumerable<TSource> source, int blockSize)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (blockSize < 1) throw new ArgumentOutOfRangeException("blockSize");

            var block = new List<TSource>();
            foreach (var x in source)
            {
                block.Add(x);
                if (block.Count == blockSize) {
                    yield return block;
                    block = new List<TSource>();
                }
            }
            if (block.Count > 0) yield return block;
        }

        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action f)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (f == null) throw new ArgumentNullException("f");
            
            foreach (var x in source)
            {
                f();
                yield return x;
            }
        }

        public static bool IsEmpty<TSource>(this IEnumerable<TSource> source)
        {
            return !source.Any();
        }
    }
}
