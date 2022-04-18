using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    [TestClass()]
    public class RDR2LocationParserTest
    {
        [TestMethod()]
        public void Test()
        {
            RDR2LocationParser parser = new RDR2LocationParser();
            parser.Parse("TuMbLeWeed\nWeS7 EIl2abeth\n 10:00 AM | 3C");
            Assert.AreEqual("Tall Trees, West Elizabeth", parser.Get());
            parser.Parse("Tall Trees\nWest Elizabeth\n 10:00 AM | 3C");
            Assert.AreEqual("Tall Trees, West Elizabeth", parser.Get());
            parser.Parse("Tumbleweed\nWest Elizabeth\n 10:00 AM | 3C");
            Assert.AreEqual("Tall Trees, West Elizabeth", parser.Get());
            parser.Parse("Ak)\n\n9 Gain control of the train car from COCOKK2019\nA\n\"");
            Assert.AreEqual("Tall Trees, West Elizabeth", parser.Get());
        }

        private static string? Parse(string text)
        {
            RDR2LocationParser parser = new RDR2LocationParser();
            parser.Parse(text);
            return parser.Get();
        }
    }
}