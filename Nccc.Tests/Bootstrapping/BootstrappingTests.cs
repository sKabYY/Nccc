using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests.Bootstrapping
{
    [TestClass]
    public class BootstrappingTests
    {
        private static NcParser _P2P(NcParser ncParser, string grammer, Action<NcParser.Settings> init = null)
        {
            var ast = ncParser.ScanAndParse(grammer).Nodes.First();
            return NcParser.Load(ast, init);
        }

        [TestMethod]
        public void TestSelf()
        {
            var ncccParser0 = new NcPGP();
            var ncGrammer = NcPGP.GetNcGrammerSource();
            var ncccParser1 = NcParser.Load(ncGrammer);
            var ncccParser2 = _P2P(ncccParser1, ncGrammer);
            var pr0 = ncccParser0.ScanAndParse(ncGrammer);
            var pr1 = ncccParser1.ScanAndParse(ncGrammer);
            var pr2 = ncccParser2.ScanAndParse(ncGrammer);
            Assert.IsTrue(pr0.IsSuccess());
            Assert.IsTrue(pr1.IsSuccess());
            Assert.IsTrue(pr2.IsSuccess());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr1).Count());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr2).Count());
        }

        [TestMethod]
        public void TestErr()
        {
            var ncccParser0 = new NcPGP();
            var ncGrammer = NcPGP.GetNcGrammerSource();
            var ncccParser1 = NcParser.Load(ncGrammer);
            var ncccParser2 = _P2P(ncccParser1, ncGrammer);
            var errGrammer = @"::dummy @hehe '123'";
            var pr0 = ncccParser0.ScanAndParse(errGrammer);
            var pr1 = ncccParser1.ScanAndParse(errGrammer);
            var pr2 = ncccParser2.ScanAndParse(errGrammer);
            Assert.IsFalse(pr0.IsSuccess());
            Assert.IsFalse(pr1.IsSuccess());
            Assert.IsFalse(pr2.IsSuccess());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr1).Count());
            Assert.AreEqual(0, Utils.DiffAndShow(pr0, pr2).Count());
        }

        private void Bootstrap3Test(string grammerPath, string samplePath)
        {
            var ncGrammer = NcPGP.GetNcGrammerSource();
            var ncccParser1 = NcParser.Load(ncGrammer);
            var ncccParser2 = _P2P(ncccParser1, ncGrammer);
            var testGrammer = Utils.ReadFromAssembly(grammerPath);
            var jsonParser0 = NcParser.Load(testGrammer);
            var jsonParser1 = _P2P(ncccParser1, testGrammer);
            var jsonParser2 = _P2P(ncccParser2, testGrammer);
            var testSample = Utils.ReadFromAssembly(samplePath);
            var pr0 = jsonParser0.ScanAndParse(testSample);
            var pr1 = jsonParser1.ScanAndParse(testSample);
            var pr2 = jsonParser2.ScanAndParse(testSample);
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
        public void TestCharMode()
        {
            Bootstrap3Test("Nccc.Tests.CharMode.charMode.grammer", "Nccc.Tests.CharMode.sample.txt");
        }
    }
}
