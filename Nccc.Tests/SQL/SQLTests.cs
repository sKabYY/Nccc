using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Linq;

namespace Nccc.Tests.SQL
{
    [TestClass]
    public class SQLTests
    {
        private readonly string _sql = Utils.ReadFromAssembly("Nccc.Tests.SQL.sample.sql");
        private readonly string _errSql = Utils.ReadFromAssembly("Nccc.Tests.SQL.sample-err.sql");

        private static NcParser _GetSqlParser()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return NcParser.LoadFromAssembly(assembly, "Nccc.Tests.SQL.sql.grammer", settings =>
            {
                settings.CaseSensitive = false;
                settings.Scanner.Delims = new string[] { "(", ")", "[", "]", "{", "}", ",", ".", ";" };
                settings.Scanner.QuotationMarks = new string[] { "\'" };
                settings.Scanner.LineComment = new string[] { "--" };
                settings.Scanner.CommentStart = "/*";
                settings.Scanner.CommentEnd = "*/";
                settings.Scanner.LispChar = new string[] { };
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
        public void TestErr()
        {
            var parser = _GetSqlParser();
            var parseResult = parser.ScanAndParse(_errSql);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Node.DigNode(parseResult.Nodes, "create_table");
            Assert.IsTrue(parseResult.Nodes.Count(n => n.Type == "comment") > 0);
            Assert.IsFalse(parseResult.IsSuccess());
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
