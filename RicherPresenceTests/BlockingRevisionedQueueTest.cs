using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Test
{
    [TestClass()]
    public class BlockingRevisionedQueueTest
    {
        [TestMethod()]
        public void TestInOrder()
        {
            BlockingRevisionedQueue<int> queue = new BlockingRevisionedQueue<int>(10, value => value, 1000 * 10, 0);
            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.Enqueue(0));
            Assert.IsTrue(queue.Enqueue(1));
            Assert.IsTrue(queue.Enqueue(2));
            Assert.IsTrue(queue.Enqueue(3));
            Assert.IsTrue(queue.Enqueue(4));
            Assert.IsTrue(queue.Enqueue(5));
            Assert.IsTrue(queue.Enqueue(6));
            Assert.IsTrue(queue.Enqueue(7));
            Assert.IsTrue(queue.Enqueue(8));
            Assert.IsTrue(queue.Enqueue(9));
            Assert.AreEqual(10, queue.Count);
            Assert.AreEqual(0, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(5, queue.Dequeue());
            Assert.AreEqual(6, queue.Dequeue());
            Assert.AreEqual(7, queue.Dequeue());
            Assert.AreEqual(8, queue.Dequeue());
            Assert.AreEqual(9, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod()]
        public void TestOutOfOrder()
        {
            BlockingRevisionedQueue<int> queue = new BlockingRevisionedQueue<int>(10, value => value, 1000 * 10, 0);
            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.Enqueue(1));
            Assert.IsTrue(queue.Enqueue(2));
            Assert.IsTrue(queue.Enqueue(5));
            Assert.IsTrue(queue.Enqueue(6));
            Assert.IsTrue(queue.Enqueue(3));
            Assert.IsTrue(queue.Enqueue(0));
            Assert.IsTrue(queue.Enqueue(4));
            Assert.IsTrue(queue.Enqueue(7));
            Assert.IsTrue(queue.Enqueue(9));
            Assert.IsTrue(queue.Enqueue(8));
            Assert.AreEqual(10, queue.Count);
            Assert.AreEqual(0, queue.Dequeue());
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(4, queue.Dequeue());
            Assert.AreEqual(5, queue.Dequeue());
            Assert.AreEqual(6, queue.Dequeue());
            Assert.AreEqual(7, queue.Dequeue());
            Assert.AreEqual(8, queue.Dequeue());
            Assert.AreEqual(9, queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod()]
        public void TestOutOfOrderAsync()
        {
            int pauseCount = 10;
            int pauseTime = 100;

            int[] values = new int[1000];
            for (int i = 0; i < values.Length; i++) values[i] = i;
            for (int i = 0; i < values.Length; i++)
            {
                int index0 = new Random().Next(values.Length);
                int index1 = new Random().Next(values.Length);
                int value = values[index0];
                values[index0] = values[index1];
                values[index1] = value;
            }
            BlockingRevisionedQueue<int> queue = new BlockingRevisionedQueue<int>(values.Length, value => value, 1000 * 10 + pauseCount * pauseTime, 0);

            Thread producer = new Thread(() =>
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i % (values.Length / pauseCount) == 0) Thread.Sleep(pauseTime);
                    queue.Enqueue(values[i]);
                }
            }) { Name = "Producer" };

            producer.Start();

            for (int i = 0; i < values.Length; i++)
            {
                Assert.AreEqual(i, queue.Dequeue());
            }
            Assert.AreEqual(0, queue.Count);
        }

    }
}