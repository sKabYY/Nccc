using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nccc.Tests
{
    [TestClass]
    public class SExpTests
    {
        public const string grammer = @"
:: sexps

sexps = (@+ sexp)
sexp = (@or item o:(open sexps close))

open = (@or '(' '[')
close = (@or ')' ']')
item = (@! (@or open close)) <*>
";

        [TestMethod]
        public void TestNcPGP()
        {
            var parser = new NcPGP();
            var parseResult = parser.ScanAndParse(grammer);
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
            var parseResult = parser.ScanAndParse(sexpCode);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }
    }
}
