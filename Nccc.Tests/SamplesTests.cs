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
spacing = ~(@* \space)
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

        private void ParseByAndPrint(string grammer, string parserName, string source)
        {
            var postProcessedGrammer = @"
::root
@include-builtin
spacing = ~(@* \space)
" + grammer;
            var parser = NcParser.Load(postProcessedGrammer);
            Console.WriteLine($"source: \"{source}\"");
            var result = parser.ParseBy(parserName, source);
            Console.WriteLine(result.ToSExp().ToPrettyString());
            Assert.IsTrue(result.IsSuccess());
        }

        [TestMethod]
        public void ParseBy()
        {
            var grammer = @"
root = header footer
header = (@+ num:number)
footer = (@+ var:(@+ alpha) spacing)
";
            ParseAndPrint(grammer: grammer, source: "1.1 1.2 abc ef");
            ParseByAndPrint(grammer: grammer, parserName: "header", source: "1.1 1.2");
            ParseByAndPrint(grammer: grammer, parserName: "footer", source: "abc ef");
        }

        [TestMethod]
        public void Default_Spacing_Should_Be_Globbed()
        {
            var grammer = @"
root = header footer
header = (@+ num:number)
footer = (@+ var:(@+ alpha) spacing)
";
            var postProcessedGrammer = @"
::root
@include-builtin
spacing = ~(@* \space)
" + grammer;
            var parser = NcParser.Load(postProcessedGrammer);
            var source = "1.1 1.2 abc ef";
            Console.WriteLine($"source: \"{source}\"");
            var result = parser.Parse(source);
            Console.WriteLine(result.ToSExp().ToPrettyString());
            Assert.IsTrue(result.IsSuccess());
            Assert.IsFalse(result.Nodes.First().StringValue().EndsWith(' '));
        }

        [TestMethod]
        public void EofTests()
        {
            var grammer = @"
root = (@+ num:number)
";
            var postProcessedGrammer = @"
::root
@include-builtin
spacing = ~(@* \space)
" + grammer;
            var parser = NcParser.Load(postProcessedGrammer);
            var source = "1.1 1.2 abc ef";
            Console.WriteLine($"source: \"{source}\"");
            var result = parser.Parse(source);
            Console.WriteLine(result.ToSExp().ToPrettyString());
            Assert.IsFalse(result.IsSuccess());
            Assert.IsTrue(result.Message.Contains("EOF"));
        }
    }
}
