using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nccc.Parser;

namespace Nccc.Tests
{
    [TestClass]
    public class SExpTests
    {
        public const string grammer = @"
:: sexps

@include-builtin

sexps = (@+ sexp)
sexp = (@or o:(open sexps close) i:item)

open = (@or '[' '(')
close = (@or ']' ')')
item = (@+ (@! (@or open close \space)) <*>) spacing

spacing = ~(@* \space)
";

        [TestMethod]
        public void TestNcPGP()
        {
            var parser = new NcGrammerParser();
            var parseResult = parser.Parse(grammer);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }

        [TestMethod]
        public void TestParse()
        {
            var parser = NcParser.Load(grammer);
            var sexpCode = @"
(define (double x) (+ x x))
(define (gcd a b) (if (= a 0) b (gcd (remainder b a) a)))
";
            var parseResult = parser.Parse(sexpCode);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }
    }
}
