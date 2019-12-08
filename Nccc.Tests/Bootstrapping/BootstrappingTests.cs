using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nccc.Common;
using Nccc.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NcGP = Nccc.Parser.NcGrammerParser;

namespace Nccc.Tests.Bootstrapping
{
    [TestClass]
    public class BootstrappingTests
    {
        private Assembly assembly = Assembly.GetExecutingAssembly();

        private static NcParser _P2P(NcParser ncParser, string grammer, Action<NcParser.Settings> init = null)
        {
            var ast = ncParser.Parse(grammer).Nodes.First();
            return NcParser.Load(ast, init);
        }

        [TestMethod]
        public void TestSelf()
        {
            var ncccParser0 = new NcGP();
            var ncGrammer = NcGP.GetNcGrammerSource();
            var ncccParser1 = NcParser.Load(ncGrammer);
            var ncccParser2 = _P2P(ncccParser1, ncGrammer);
            var pr0 = ncccParser0.Parse(ncGrammer);
            Assert.IsTrue(pr0.IsSuccess());
            var pr1 = ncccParser1.Parse(ncGrammer);
            Assert.IsTrue(pr1.IsSuccess());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr1).Count());
            var pr2 = ncccParser2.Parse(ncGrammer);
            Assert.IsTrue(pr2.IsSuccess());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr2).Count());
        }

        [TestMethod]
        public void TestErr()
        {
            var ncccParser0 = new NcGP();
            var ncGrammer = NcGP.GetNcGrammerSource();
            var ncccParser1 = NcParser.Load(ncGrammer);
            var ncccParser2 = _P2P(ncccParser1, ncGrammer);
            var errGrammer = @"::dummy @hehe '123'";
            var pr0 = ncccParser0.Parse(errGrammer);
            var pr1 = ncccParser1.Parse(errGrammer);
            var pr2 = ncccParser2.Parse(errGrammer);
            Assert.IsFalse(pr0.IsSuccess());
            Assert.IsFalse(pr1.IsSuccess());
            Assert.IsFalse(pr2.IsSuccess());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr1).Count());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr2).Count());
        }

        private void Bootstrap3Test(string grammerPath, string samplePath)
        {
            var ncGrammer = NcGP.GetNcGrammerSource();
            var ncccParser1 = NcParser.Load(ncGrammer);
            var ncccParser2 = _P2P(ncccParser1, ncGrammer);
            var testGrammer = assembly.ReadString(grammerPath);
            var jsonParser0 = NcParser.Load(testGrammer);
            var jsonParser1 = _P2P(ncccParser1, testGrammer);
            var jsonParser2 = _P2P(ncccParser2, testGrammer);
            var testSample = assembly.ReadString(samplePath);
            var pr0 = jsonParser0.Parse(testSample);
            var pr1 = jsonParser1.Parse(testSample);
            var pr2 = jsonParser2.Parse(testSample);
            Assert.IsTrue(pr0.IsSuccess());
            Assert.IsTrue(pr1.IsSuccess());
            Assert.IsTrue(pr2.IsSuccess());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr1).Count());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr2).Count());
        }

        [TestMethod]
        public void TestJson()
        {
            Bootstrap3Test("Nccc.Tests.Json.json.grammer", "Nccc.Tests.Json.sample.json");
        }

        [TestMethod]
        public void TestLexMode()
        {
            Bootstrap3Test("Nccc.Tests.LexMode.lexMode.grammer", "Nccc.Tests.LexMode.sample.txt");
        }
    }
}
