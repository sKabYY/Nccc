using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nccc.Parser;
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
        private void ParseAndPrint(string grammer, string source)
        {
            var postProcessedGrammer = @"
::root
@include-builtin
" + grammer;
            var parser = NcParser.Load(postProcessedGrammer);
            Console.WriteLine($"source: \"{source}\"");
            var result = parser.Parse(source);
            Console.WriteLine(result.ToSExp().ToPrettyString());
            Assert.IsTrue(result.IsSuccess());
        }

        [TestMethod]
        public void ParseString()
        {
            ParseAndPrint(grammer: "root = 'A'", source: "A");
        }

        [TestMethod]
        public void ParseSeq()
        {
            ParseAndPrint(grammer: "root = (@.. 'A' 'B')", source: "A B");
            ParseAndPrint(grammer: "root = ('A' 'B')", source: "A B");
            ParseAndPrint(grammer: "root = 'A' 'B'", source: "A B");
        }

        [TestMethod]
        public void ParseOr()
        {
            var grammer = "root = (@or 'A' 'B')";
            ParseAndPrint(grammer: grammer, source: "A");
            ParseAndPrint(grammer: grammer, source: "B");
        }
        [TestMethod]
        public void ParseMaybe()
        {
            var grammer = "root = (@? 'A')";
            ParseAndPrint(grammer: grammer, source: "");
            ParseAndPrint(grammer: grammer, source: "A");
        }

        [TestMethod]
        public void ParseStar()
        {
            var grammer = "root = (@* 'A')";
            ParseAndPrint(grammer: grammer, source: "");
            ParseAndPrint(grammer: grammer, source: "A A A A");
        }

        [TestMethod]
        public void ParseStarAOrB()
        {
            var grammer = "root = (@* (@or 'A' 'B'))";
            ParseAndPrint(grammer: grammer, source: "B B");
            ParseAndPrint(grammer: grammer, source: "A A");
            ParseAndPrint(grammer: grammer, source: "A B");
            ParseAndPrint(grammer: grammer, source: "A B A A");
        }

        [TestMethod]
        public void ParseNumber()
        {
            ParseAndPrint(grammer: "root = number", source: "1.1");
        }

        [TestMethod]
        public void ParseNumberOrStringA()
        {
            var grammer = "root = (@or number 'A')";
            ParseAndPrint(grammer: grammer, source: "1.1");
            ParseAndPrint(grammer: grammer, source: "A");
        }

        [TestMethod]
        public void ParseStarNumberOrStringA()
        {
            var grammer = "root = (@* (@or number #'A'))";
            ParseAndPrint(grammer: grammer, source: "1.1");
            ParseAndPrint(grammer: grammer, source: "A");
            ParseAndPrint(grammer: grammer, source: "A A");
            ParseAndPrint(grammer: grammer, source: "1.1 2");
            ParseAndPrint(grammer: grammer, source: "A 23");
            ParseAndPrint(grammer: grammer, source: "A 1.2 A A");
        }
    }
}
