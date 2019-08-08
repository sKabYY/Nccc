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
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Json.json.grammer");
            var parseResult = parser.ScanAndParse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }

        [TestMethod]
        public void TestError()
        {
            var src = "[1,2,3";
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Json.json.grammer", settings =>
            {
                settings.Language = "zh-cn";
            });
            var parseResult = parser.ScanAndParse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsFalse(parseResult.IsSuccess());
        }
    }
}
