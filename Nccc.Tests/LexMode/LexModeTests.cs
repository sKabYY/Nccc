using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nccc.Common;
using Nccc.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests.LexMode
{
    [TestClass]
    public class LexModeTests
    {
        [TestMethod]
        public void TestSample()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var src = assembly.ReadString("Nccc.Tests.LexMode.sample.txt");
            var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.LexMode.lexMode.grammer");
            var parseResult = parser.Parse(src);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }
    }
}
