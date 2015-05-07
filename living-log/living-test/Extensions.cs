using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using living_log_cli;
using System.Linq;

namespace living_test
{
    [TestClass]
    public class Extensions
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_ReadBlocks_Null()
        {
            List<int> list = null;

            list.PartitionBlocks(1).ToList();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Test_ReadBlocks_EmptyBlocks()
        {
            List<int> list = new List<int>();

            list.PartitionBlocks(0).ToList();
        }

        [TestMethod]
        public void Test_ReadBlocks_Empty()
        {
            List<int> list = new List<int>();

            var blocks = list.PartitionBlocks(1);

            Assert.IsNotNull(blocks);
            Assert.IsTrue(blocks.IsEmpty());
        }

        [TestMethod]
        public void Test_ReadBlocks_SingleBlock_Small()
        {
            var items = Enumerable.Range(0, 5);

            var blocks = items.PartitionBlocks(10);

            Assert.IsNotNull(blocks);
            Assert.AreEqual(1, blocks.Count());
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(0), items));
        }

        [TestMethod]
        public void Test_ReadBlocks_SingleBlock_Full()
        {
            var items = Enumerable.Range(0, 10);

            var blocks = items.PartitionBlocks(10);

            Assert.IsNotNull(blocks);
            Assert.AreEqual(1, blocks.Count());
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(0), items));
        }

        [TestMethod]
        public void Test_ReadBlocks_TripleBlock_Small()
        {
            var items = Enumerable.Range(0, 25);

            var blocks = items.PartitionBlocks(10);

            Assert.IsNotNull(blocks);
            Assert.AreEqual(3, blocks.Count());
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(0), Enumerable.Range( 0, 10)));
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(1), Enumerable.Range(10, 10)));
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(2), Enumerable.Range(20,  5)));
        }

        [TestMethod]
        public void Test_ReadBlocks_TripleBlock_Full()
        {
            var items = Enumerable.Range(0, 30);

            var blocks = items.PartitionBlocks(10);

            Assert.IsNotNull(blocks);
            Assert.AreEqual(3, blocks.Count());
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(0), Enumerable.Range( 0, 10)));
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(1), Enumerable.Range(10, 10)));
            Assert.IsTrue(Enumerable.SequenceEqual(blocks.ElementAt(2), Enumerable.Range(20, 10)));
        }
    }
}
