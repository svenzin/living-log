using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using living_log_cli;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace living_log_tests
{
    [TestClass]
    public class Test_EncodedReaderWriter
    {
        [TestMethod]
        public void Encoded_UInt64RoundTrip()
        {
            List<ulong> numbers = new List<ulong>() {
                0,
                1,
                ulong.MinValue,
                ulong.MaxValue,
            }
            .Concat(Enumerable.Range(1, 63).Select(n => 1UL << n))
            .Concat(Enumerable.Range(0, 1 << 16).Select(n => (ulong)n))
            .ToList();

            byte[] buffer;

            using (var stream = new MemoryStream())
            {
                using (var writer = new Program.EncodedWriter(stream))
                {
                    foreach (var n in numbers)
                    {
                        writer.WriteEncoded(n);
                    }

                    buffer = stream.ToArray();
                }
            }

            using (var stream = new MemoryStream(buffer))
            {
                using (var reader = new Program.EncodedReader(stream))
                {
                    foreach (var n in numbers)
                    {
                        ulong value = reader.ReadEncodedUInt64();
                        Assert.AreEqual(n, value);
                    }
                }
            }
        }

        [TestMethod]
        public void Encoded_Int64RoundTrip()
        {
            List<long> numbers = new List<long>() {
                0,
                1,
                long.MinValue,
                long.MaxValue,
            }
            .Concat(Enumerable.Range(1, 62).Select(n => 1L << n))
            .Concat(Enumerable.Range(1, 62).Select(n => -(1L << n)))
            .Concat(Enumerable.Range(-(1 << 16), 1 << 16).Select(n => (long)n))
            .ToList();

            byte[] buffer;

            using (var stream = new MemoryStream())
            {
                using (var writer = new Program.EncodedWriter(stream))
                {
                    foreach (var n in numbers)
                    {
                        writer.WriteEncoded(n);
                    }

                    buffer = stream.ToArray();
                }
            }

            using (var stream = new MemoryStream(buffer))
            {
                using (var reader = new Program.EncodedReader(stream))
                {
                    foreach (var n in numbers)
                    {
                        long value = reader.ReadEncodedInt64();
                        Assert.AreEqual(n, value);
                    }
                }
            }
        }
    }
}
