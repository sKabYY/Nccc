using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;

namespace Nccc.Tests.SQL
{
    [TestClass]
    public class SQLTests
    {
        private readonly string _sql = Utils.ReadFromAssembly("Nccc.Tests.SQL.sample.sql");

        private static NcParser _GetSqlParser()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return NcParser.LoadFromAssembly(assembly, "Nccc.Tests.SQL.sql.grammer", p =>
            {
                p.CaseSensitive = false;
                p.Scanner.Delims = new string[] { "(", ")", "[", "]", "{", "}", ",", ".", ";" };
                p.Scanner.QuotationMarks = new string[] { "\'" };
                p.Scanner.LineComment = new string[] { "--" };
                p.Scanner.CommentStart = "/*";
                p.Scanner.CommentEnd = "*/";
                p.Scanner.LispChar = new string[] { };
            });
        }

        [TestMethod]
        public void Test()
        {
            var parser = _GetSqlParser();
            var parseResult = parser.ScanAndParse(_sql);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }

        [TestMethod]
        public void Test100Times()
        {
            var times = 100;
            var parser = _GetSqlParser();
            for (var i = 0; i < times; ++i)
            {
                var parseResult = parser.ScanAndParse(_sql);
                Assert.IsTrue(parseResult.IsSuccess());
            }
        }
    }
}
