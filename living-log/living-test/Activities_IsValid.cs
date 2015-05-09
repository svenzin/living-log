using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using living_log_cli;

namespace living_test
{
    [TestClass]
    public class Activities_IsValid
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Null()
        {
            ActivityTools.IsValid(null);
        }

        [TestMethod]
        public void Test_Empty()
        {
            var activities = new List<Activity>();
            
            Assert.IsTrue(ActivityTools.IsValid(activities));
        }

        [TestMethod]
        public void Test_FirstIsSync()
        {
            var activities = new List<Activity>();
            activities.Add(new Activity() { Type = Categories.Keyboard_KeyDown });

            Assert.IsFalse(ActivityTools.IsValid(activities));
        }

        [TestMethod]
        public void Test_HasNull()
        {
            var activities = new List<Activity>();
            activities.Add(LivingLogger.GetSync());
            activities.Add(null);

            Assert.IsFalse(ActivityTools.IsValid(activities));
        }
        
        [TestMethod]
        public void Test_NonChronological()
        {
            var t0 = DateTime.UtcNow;
            var t1 = t0 - TimeSpan.FromSeconds(1);

            var activities = new List<Activity>();
            activities.Add(LivingLogger.GetSync(new Timestamp(t0)));
            activities.Add(LivingLogger.GetSync(new Timestamp(t1)));

            Assert.IsFalse(ActivityTools.IsValid(activities));
        }

        [TestMethod]
        public void Test_Unknown()
        {
            var t0 = DateTime.UtcNow;
            var t1 = t0 + TimeSpan.FromSeconds(1);
            
            var activities = new List<Activity>();
            activities.Add(LivingLogger.GetSync(new Timestamp(t0)));
            activities.Add(new Activity()
            {
                Timestamp = new Timestamp(t1),
                Type = Category.Unknown,
            });

            Assert.IsFalse(ActivityTools.IsValid(activities));
        }
    }
}
