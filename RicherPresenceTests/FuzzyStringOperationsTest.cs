using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    [TestClass()]
    public class FuzzyStringOperationsTest
    {
        [TestMethod()]
        public void TestFuzzyEquals()
        {
            Assert.AreEqual(true, "abc".FuzzyEquals("abc", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("abcdefghijklmnopqrstuvwxyz", 0.5));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("abcdefghijklmnopqrstuvwxyz", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("abcdefghijkImnopqrstuvwxyz", 0.9));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("abcdefghijkxlmnopqrstuvwxyz", 0.9));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("xabcdefghijklmnopqrstuvwxyz", 0.9));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("abcdefghijklmnopqrstuvwxyzx", 0.9));
            Assert.AreEqual(false, "abcdefghijklmnopqrstuvwxyz".FuzzyEquals("abcdefghijklmnopqrstuvwxyzx", 0.99));
        }

        [TestMethod()]
        public void TestFuzzyContains()
        {
            Assert.AreEqual(true, "abc".FuzzyContains("abc", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyContains("abcdefghijklmnopqrstuvwxyz", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyContains("abc", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyContains("xyz", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyContains("jkl", 1));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyContains("jkI", 0.7));
            Assert.AreEqual(true, "abcdefghijklmnopqrstuvwxyz".FuzzyContains("jkm", 0.7));
        }

        [TestMethod()]
        public void TestFuzzyIndexOf()
        {
            Assert.AreEqual((0, 3), "abc".FuzzyIndexOf("abc", 1));
            Assert.AreEqual((0, 3), "abcdefghijklmnopqrstuvwxyz".FuzzyIndexOf("abc", 1));
            Assert.AreEqual((26-3, 3), "abcdefghijklmnopqrstuvwxyz".FuzzyIndexOf("xyz", 1));
            Assert.AreEqual((9, 4), "abcdefghijklmnopqrstuvwxyz".FuzzyIndexOf("jkIm", 0.7));
            Assert.AreEqual((9, 3), "abcdefghijklmnopqrstuvwxyz".FuzzyIndexOf("jklx", 0.7));
        }
    }
}