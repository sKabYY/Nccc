﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests.Json
{
    [TestClass]
    public class JsonTests
    {
        [TestMethod]
        public void TestSample()
        {
            var src = Utils.ReadFromAssembly("Nccc.Tests.Json.sample.json");
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Json.json.grammer", p =>
            {
                p.Scanner.Delims = new string[] { "{", "}", "[", "]", ",", ":" };
                p.Scanner.QuotationMarks = new string[] { "\"" };
                p.Scanner.LineComment = new string[] { };
                p.Scanner.CommentStart = null;
                p.Scanner.CommentEnd = null;
                p.Scanner.LispChar = new string[] { };
            });
            var parseResult = parser.ScanAndParse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }
    }
}
