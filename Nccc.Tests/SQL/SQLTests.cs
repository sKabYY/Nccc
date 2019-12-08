using Nccc.Common;
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Linq;
using Nccc.Parser;

namespace Nccc.Tests.SQL
{
    [TestClass]
    public class SQLTests
    {
        private readonly string _sql = Assembly.GetExecutingAssembly().ReadString("Nccc.Tests.SQL.sample.sql");
        private readonly string _errSql = Assembly.GetExecutingAssembly().ReadString("Nccc.Tests.SQL.sample-err.sql");

        private static NcParser GetSqlParser()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return NcParser.LoadFromAssembly(assembly, "Nccc.Tests.SQL.sql.grammer", settings =>
            {
                settings.CaseSensitive = false;
            });
        }

        [TestMethod]
        public void Test()
        {
            var parser = GetSqlParser();
            var parseResult = parser.Parse(_sql);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }

        [TestMethod]
        public void TestErr()
        {
            var parser = GetSqlParser();
            var parseResult = parser.Parse(_errSql);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Node.DigNode(parseResult.Nodes, "create_table");
            Assert.IsTrue(parseResult.Nodes.Count(n => n.Type == "comment") > 0);
            Assert.IsFalse(parseResult.IsSuccess());
        }

        [TestMethod]
        public void Test100Times()
        {
            var times = 100;
            var parser = GetSqlParser();
            for (var i = 0; i < times; ++i)
            {
                var parseResult = parser.Parse(_sql);
                Assert.IsTrue(parseResult.IsSuccess());
            }
        }
    }
}
