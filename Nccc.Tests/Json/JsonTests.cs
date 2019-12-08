using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nccc.Common;
using Nccc.Parser;
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
            var src = Assembly.GetExecutingAssembly().ReadString("Nccc.Tests.Json.sample.json");
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Json.json.grammer");
            var parseResult = parser.Parse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }

        [TestMethod]
        public void TestError()
        {
            var expectMessage = "哈哈";
            var src = "[1,2,3";
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Json.json.grammer", settings =>
            {
                settings.Locale.Language = "zh-cn";
                settings.Locale.Set("zh-cn", new Dictionary<string, string>
                {
                    { "expect", expectMessage }
                });
            });
            var parseResult = parser.Parse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsFalse(parseResult.IsSuccess());
            Assert.IsTrue(parseResult.Message.Contains(expectMessage));
        }
    }
}
