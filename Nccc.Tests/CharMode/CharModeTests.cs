using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests.CharMode
{
    [TestClass]
    public class CharModeTests
    {
        [TestMethod]
        public void TestSample()
        {
            var src = Utils.ReadFromAssembly("Nccc.Tests.CharMode.sample.txt");
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.CharMode.charMode.grammer");
            var parseResult = parser.ScanAndParse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }
    }
}
