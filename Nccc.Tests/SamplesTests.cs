using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests
{
    [TestClass]
    public class SamplesTests
    {
        private void _ParseAndPrint(string grammer, string source)
        {
            var parser = NcParser.Load(grammer);
            var result = parser.ScanAndParse(source);
            Console.WriteLine(result.ToSExp().ToPrettyString());
            Assert.IsTrue(result.IsSuccess());
        }

        [TestMethod]
        public void ParseString()
        {
            _ParseAndPrint(grammer: "::root\nroot = 'A'", source: "A");
        }

        [TestMethod]
        public void ParseSeq()
        {
            _ParseAndPrint(grammer: "::root\nroot = (@.. 'A' 'B')", source: "A B");
            _ParseAndPrint(grammer: "::root\nroot = ('A' 'B')", source: "A B");
            _ParseAndPrint(grammer: "::root\nroot = 'A' 'B'", source: "A B");
        }

        [TestMethod]
        public void ParseOr()
        {
            var grammer = "::root\nroot = (@or 'A' 'B')";
            _ParseAndPrint(grammer: grammer, source: "A");
            _ParseAndPrint(grammer: grammer, source: "B");
        }
        [TestMethod]
        public void ParseMaybe()
        {
            var grammer = "::root\nroot = (@? 'A')";
            _ParseAndPrint(grammer: grammer, source: "");
            _ParseAndPrint(grammer: grammer, source: "A");
        }

        [TestMethod]
        public void ParseStar()
        {
            var grammer = "::root\nroot = (@* 'A')";
            _ParseAndPrint(grammer: grammer, source: "");
            _ParseAndPrint(grammer: grammer, source: "A A A A");
        }

        [TestMethod]
        public void ParseStarAOrB()
        {
            var grammer = "::root\nroot = (@* (@or 'A' 'B'))";
            _ParseAndPrint(grammer: grammer, source: "B B");
            _ParseAndPrint(grammer: grammer, source: "A A");
            _ParseAndPrint(grammer: grammer, source: "A B");
            _ParseAndPrint(grammer: grammer, source: "A B A A");
        }

        [TestMethod]
        public void ParseNumber()
        {
            _ParseAndPrint(grammer: "::root\nroot = <number>", source: "1.1");
        }

        [TestMethod]
        public void ParseNumberOrStringA()
        {
            var grammer = "::root\nroot = (@or <number> 'A')";
            _ParseAndPrint(grammer: grammer, source: "1.1");
            _ParseAndPrint(grammer: grammer, source: "A");
        }

        [TestMethod]
        public void ParseStarNumberOrStringA()
        {
            var grammer = "::root\nroot = (@* (@or <number> 'A'))";
            _ParseAndPrint(grammer: grammer, source: "1.1");
            _ParseAndPrint(grammer: grammer, source: "A");
            _ParseAndPrint(grammer: grammer, source: "A A");
            _ParseAndPrint(grammer: grammer, source: "1.1 2");
            _ParseAndPrint(grammer: grammer, source: "A 23");
            _ParseAndPrint(grammer: grammer, source: "A 1.2 A A");
        }
    }
}
