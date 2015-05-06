using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using living_log_cli;
using System.Collections.Generic;
using System.Linq;

namespace living_test
{
    [TestClass]
    public class Activities_Merge
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Null_Null()
        {
            ActivityTools.Merge(null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Null_1()
        {
            var source = Enumerable.Empty<Activity>();
            ActivityTools.Merge(null, source);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Null_2()
        {
            var source = Enumerable.Empty<Activity>();
            ActivityTools.Merge(source, null);
        }

        [TestMethod]
        public void Test_Empty_Empty()
        {
            var source = Enumerable.Empty<Activity>();
            
            var result = ActivityTools.Merge(source, source).ToList();

            Assert.IsTrue(result.IsEmpty());
        }

        [TestMethod]
        public void Test_Empty_1()
        {
            var sourceA = Enumerable.Empty<Activity>();
            var sourceB = new List<Activity>() { LivingLogger.GetSync() };

            var result = ActivityTools.Merge(sourceA, sourceB).ToList();

            Assert.IsTrue(result.SequenceEqual(sourceB));
        }

        [TestMethod]
        public void Test_Empty_2()
        {
            var sourceA = new List<Activity>() { LivingLogger.GetSync() };
            var sourceB = Enumerable.Empty<Activity>();

            var result = ActivityTools.Merge(sourceA, sourceB).ToList();

            Assert.IsTrue(result.SequenceEqual(sourceA));
        }

        [TestMethod]
        public void Test_First_1()
        {
            var t0 = DateTime.Now;
            var t1 = t0 + TimeSpan.FromSeconds(1);

            var sourceA = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t0)) };
            var sourceB = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t1)) };

            var result = ActivityTools.Merge(sourceA, sourceB).ToList();

            Assert.IsTrue(result.SequenceEqual(sourceA.Concat(sourceB)));
        }

        [TestMethod]
        public void Test_First_2()
        {
            var t0 = DateTime.Now;
            var t1 = t0 + TimeSpan.FromSeconds(1);

            var sourceA = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t1)) };
            var sourceB = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t0)) };

            var result = ActivityTools.Merge(sourceA, sourceB).ToList();

            Assert.IsTrue(result.SequenceEqual(sourceB.Concat(sourceA)));
        }

        [TestMethod]
        public void Test_Ordering()
        {
            var t0 = DateTime.Now;
            var t1 = t0 + TimeSpan.FromSeconds(1);
            var t2 = t1 + TimeSpan.FromSeconds(1);
            var t3 = t2 + TimeSpan.FromSeconds(1);

            var sourceA = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t0)) };
            var sourceB = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t1)) };
            var sourceC = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t2)) };
            var sourceD = new List<Activity>() { LivingLogger.GetSync(new Timestamp(t3)) };

            var result = ActivityTools.Merge(
                sourceA.Concat(sourceC),
                sourceB.Concat(sourceD)
                ).ToList();

            Assert.IsTrue(result.SequenceEqual(
                sourceA.Concat(sourceB).Concat(sourceC).Concat(sourceD)
                ));
        }

        [TestMethod]
        public void Test_Uniqueness()
        {
            Assert.Fail();
        }
    }
}
