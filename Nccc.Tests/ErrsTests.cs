using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests
{
    [TestClass]
    public class ErrsTests
    {
        public const string grammer = @"
:: root

@set-message-locale-start 'L{'
@set-message-locale-end '}'

root = (@err'L{expect} A L{or} B' oo:(@or 'A' 'B'))
";
        [TestMethod]
        public void TestMessageLocale()
        {
            var parser = NcParser.Load(grammer, settings =>
            {
                settings.Locale.Language = "zh-cn";
                settings.Locale.Set("zh-cn", new Dictionary<string, string>
                {
                    { "expect", "盼望着" },
                    { "or", "或" },
                });
            });
            var source = "C";
            var result = parser.ScanAndParse(source);
            Console.WriteLine(result.ToSExp().ToPrettyString());
            Assert.IsFalse(result.IsSuccess());
            Assert.AreEqual("盼望着 A 或 B", result.Message);
        }
    }
}
