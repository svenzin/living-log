using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using living_log_cli;
using System.Collections.Generic;
using System.IO;

namespace living_log_tests
{
    [TestClass]
    public class Test_Converter
    {
        [TestMethod]
        public void UInt64_RoundTrip()
        {
            for (ulong n = 0; n <= (1 << 16); ++n)
            {
                Assert.AreEqual(n, Converter.Convert(Converter.Convert(n)));
            }
            Assert.AreEqual(ulong.MinValue, Converter.Convert(Converter.Convert(ulong.MinValue)));
            Assert.AreEqual(ulong.MaxValue, Converter.Convert(Converter.Convert(ulong.MaxValue)));
        }

        [TestMethod]
        public void Int64_RoundTrip()
        {
            for (long n = -(1 << 16); n <= (1 << 16); ++n)
            {
                Assert.AreEqual(n, Converter.Convert(Converter.Convert(n)));
            }
            Assert.AreEqual(long.MinValue, Converter.Convert(Converter.Convert(long.MinValue)));
            Assert.AreEqual(long.MaxValue, Converter.Convert(Converter.Convert(long.MaxValue)));
        }
    }
}
