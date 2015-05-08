using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using living_log_cli;
using System.Linq;
using System.Collections.Generic;

namespace living_test
{
    [TestClass]
    public class Activities_Partition
    {
        class FuncComparer<T> : IEqualityComparer<T>
        {
            Func<T, T, bool> _c;
            Func<T, int> _h;

            public FuncComparer(Func<T, T, bool> comparer) : this(comparer, x => 0) { }
            public FuncComparer(Func<T, T, bool> comparer, Func<T, int> hasher) { _c = comparer; _h = hasher; }

            public bool Equals(T x, T y) { return _c(x, y); }
            public int GetHashCode(T x) { return _h(x); }
        }
        bool PartitionEqual(IEnumerable<IEnumerable<Activity>> expected, IEnumerable<IEnumerable<Activity>> result)
        {
            return expected.SequenceEqual(result, new FuncComparer<IEnumerable<Activity>>((e, r) => e.SequenceEqual(r)));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Chronological_Null()
        {
            ActivityTools.PartitionChronological(null).ToList();
        }

        [TestMethod]
        public void Test_Chronological_Empty()
        {
            var empty = Enumerable.Empty<Activity>();
            var result = ActivityTools.PartitionChronological(empty);
            Assert.IsTrue(result.IsEmpty());
        }

        [TestMethod]
        public void Test_Chronological_Single()
        {
            var a = LivingLogger.GetSync();
            var activities = Enumerable.Repeat(a, 1);
            var result = ActivityTools.PartitionChronological(activities);
            var expected = Enumerable.Repeat(activities, 1);
            Assert.IsTrue(PartitionEqual(expected, result));
        }

        [TestMethod]
        public void Test_Chronological_Dual()
        {
            var t0 = DateTime.UtcNow;
            var t1 = t0 + TimeSpan.FromSeconds(1);
            var t2 = t1 + TimeSpan.FromSeconds(1);
            var t3 = t2 + TimeSpan.FromSeconds(1);

            var a0 = LivingLogger.GetSync(new Timestamp(t0));
            var a1 = LivingLogger.GetSync(new Timestamp(t1));
            var a2 = LivingLogger.GetSync(new Timestamp(t2));
            var a3 = LivingLogger.GetSync(new Timestamp(t3));

            var actA = new List<Activity>() { a0, a2 };
            var actB = new List<Activity>() { a1, a3 };

            var activities = actA.Concat(actB);

            var result = ActivityTools.PartitionChronological(activities);

            var expected = new List<List<Activity>>() { actA, actB };
            Assert.IsTrue(PartitionEqual(expected, result));
        }
        
        [TestMethod]
        public void Test_Chronological_AntiChrono()
        {
            var t0 = DateTime.UtcNow;
            var t1 = t0 + TimeSpan.FromSeconds(1);
            var t2 = t1 + TimeSpan.FromSeconds(1);
            var t3 = t2 + TimeSpan.FromSeconds(1);

            var a0 = LivingLogger.GetSync(new Timestamp(t0));
            var a1 = LivingLogger.GetSync(new Timestamp(t1));
            var a2 = LivingLogger.GetSync(new Timestamp(t2));
            var a3 = LivingLogger.GetSync(new Timestamp(t3));

            var act0 = Enumerable.Repeat(a0, 1);
            var act1 = Enumerable.Repeat(a1, 1);
            var act2 = Enumerable.Repeat(a2, 1);
            var act3 = Enumerable.Repeat(a3, 1);

            var activities = act3.Concat(act2).Concat(act1).Concat(act0);

            var result = ActivityTools.PartitionChronological(activities);

            var expected = new List<IEnumerable<Activity>>() { act3, act2, act1, act0 };
            Assert.IsTrue(PartitionEqual(expected, result));
        }
    }
}
