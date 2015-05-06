using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using living_log_cli;

namespace living_test
{
    [TestClass]
    public class Activities_RemoveDuplicates
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Null()
        {
            ActivityTools.RemoveDuplicates(null);
        }

        [TestMethod]
        public void Test_NoDuplicates()
        {
            var t0 = new DateTime(2000, 01, 01);
            var a0 = new Activity()
            {
                Timestamp = new Timestamp(t0),
                Type = Categories.LivingLog_Startup,
                Info = LivingLogger.SyncData.At(t0)
            };

            var t1 = new DateTime(2015, 05, 06);
            var a1 = new Activity()
            {
                Timestamp = new Timestamp(t1),
                Type = Categories.LivingLog_Exit,
                Info = LivingLogger.SyncData.At(t1)
            };

            var source = new List<Activity>() { a0, a1 };

            var result = ActivityTools.RemoveDuplicates(source);

            Assert.IsTrue(result.SequenceEqual(source));
        }

        [TestMethod]
        public void Test_Duplicate_Reference()
        {
            var t0 = new DateTime(2000, 01, 01);
            var a0 = new Activity()
            {
                Timestamp = new Timestamp(t0),
                Type = Categories.LivingLog_Startup,
                Info = LivingLogger.SyncData.At(t0)
            };

            var source = new List<Activity>() { a0, a0 };

            var result = ActivityTools.RemoveDuplicates(source);

            var expected = source.Take(1);

            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void Test_Duplicate_Value()
        {
            var t0 = new DateTime(2000, 01, 01);
            var a0 = new Activity()
            {
                Timestamp = new Timestamp(t0),
                Type = Categories.LivingLog_Startup,
                Info = LivingLogger.SyncData.At(t0)
            };

            var a1 = new Activity()
            {
                Timestamp = a0.Timestamp,
                Type = a0.Type,
                Info = a0.Info
            };

            var source = new List<Activity>() { a0, a1 };

            var result = ActivityTools.RemoveDuplicates(source);

            Assert.IsTrue(result.SequenceEqual(source.Take(1)));
            Assert.IsTrue(result.SequenceEqual(source.Skip(1)));
        }

        [TestMethod]
        public void Test_NoDuplicate_ByTimestamp()
        {
            var t0 = new DateTime(2000, 01, 01);
            var a0 = new Activity()
            {
                Timestamp = new Timestamp(t0),
                Type = Categories.LivingLog_Startup,
                Info = LivingLogger.SyncData.At(t0)
            };

            var t1 = new DateTime(2015, 06, 05);
            var a1 = new Activity()
            {
                Timestamp = new Timestamp(t1),
                Type = a0.Type,
                Info = a0.Info
            };

            var source = new List<Activity>() { a0, a1 };

            var result = ActivityTools.RemoveDuplicates(source);

            Assert.IsTrue(result.SequenceEqual(source));
        }

        [TestMethod]
        public void Test_NoDuplicate_ByType()
        {
            var t0 = new DateTime(2000, 01, 01);
            var a0 = new Activity()
            {
                Timestamp = new Timestamp(t0),
                Type = Categories.LivingLog_Startup,
                Info = LivingLogger.SyncData.At(t0)
            };

            var a1 = new Activity()
            {
                Timestamp = a0.Timestamp,
                Type = Categories.LivingLog_Sync,
                Info = a0.Info
            };

            var source = new List<Activity>() { a0, a1 };

            var result = ActivityTools.RemoveDuplicates(source);

            Assert.IsTrue(result.SequenceEqual(source));
        }

        [TestMethod]
        public void Test_NoDuplicate_ByInfo()
        {
            var t0 = new DateTime(2000, 01, 01);
            var a0 = new Activity()
            {
                Timestamp = new Timestamp(t0),
                Type = Categories.LivingLog_Startup,
                Info = LivingLogger.SyncData.At(t0)
            };

            var t1 = new DateTime(2015, 06, 05);
            var a1 = new Activity()
            {
                Timestamp = a0.Timestamp,
                Type = a0.Type,
                Info = LivingLogger.SyncData.At(t1)
            };

            var source = new List<Activity>() { a0, a1 };

            var result = ActivityTools.RemoveDuplicates(source);

            Assert.IsTrue(result.SequenceEqual(source));
        }
    }
}
