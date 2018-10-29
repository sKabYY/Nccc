using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nccc.Tests
{
    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public void Test()
        {
            var s = "; a'bc'd\n" +
                "aa(cc\"kk\\\"kk\"(\"ss\")aa;c\n" +
                "/*te\nst*/;ac\n";
            Console.WriteLine(s);
            var scanner = new Scanner
            {
                CommentStart = "/*",
                CommentEnd = "*/"
            };
            var stream = scanner.Scan(s);
            var toks = stream.ToList().Select(t => t.ToString()).ToList();
            Console.WriteLine($"[{string.Join(", ", toks)}]");
            Assert.AreEqual(toks.Count, 12);
            var toksWithoutComment = stream.FilterComment().ToList().Select(t => t.ToString()).ToList();
            Console.WriteLine($"[{string.Join(", ", toksWithoutComment)}]");
            var numNotComment = toks.Where(tok => !tok.StartsWith("<Token type=Comment")).Count();
            Assert.AreEqual(toksWithoutComment.Count, numNotComment);
        }
    }
}
