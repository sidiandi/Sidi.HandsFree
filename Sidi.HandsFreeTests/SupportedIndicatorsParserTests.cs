using NUnit.Framework;
using Sidi.HandsFree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprache;

namespace Sidi.HandsFree.Tests
{
    [TestFixture()]
    public class SupportedIndicatorsParserTests
    {
        [Test()]
        public void SupportedIndicatorsParserTest()
        {
            var input = @"(""call"",(0,1)),(""callsetup"",(0-3)),(""service"",(0-1)),(""signal"",(0-5)),(""roam"",(0,1)),(""battchg"",(0-5)),(""callheld"",(0-2))";
            var indicator = SupportedIndicatorsParser.Parse(input);
            Assert.AreEqual(7, indicator.Count());
        }

        [Test]
        public void ValueList()
        {
            var input = "0,0,\"Vodafone\"";
            var r = SupportedIndicatorsParser.CommaSeparatedStrings.Parse(input);
            Assert.AreEqual("0", r[0]);
            Assert.AreEqual("0", r[1]);
            Assert.AreEqual("Vodafone", r[2]);
        }
    }
}