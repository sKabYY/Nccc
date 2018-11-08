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
        private static NcParser _P2P(NcParser ncParser, string grammer, Action<NcParser> init = null)
        {
            var ast = ncParser.ScanAndParse(grammer).Nodes.First();
            return NcParser.Load(ast, init);
        }

        [TestMethod]
        public void TestSelf()
        {
            var ncGrammer = NcPGP.GetNcGrammerSource();
            var ncParser1 = NcParser.Load(ncGrammer);
            var ncParser2 = _P2P(ncParser1, ncGrammer);
            var pr1 = ncParser1.ScanAndParse(ncGrammer);
            var pr2 = ncParser2.ScanAndParse(ncGrammer);
            Assert.IsTrue(pr1.IsSuccess());
            Assert.IsTrue(pr2.IsSuccess());
            Assert.AreEqual(pr1.ToSExp().ToPrettyString(), pr1.ToSExp().ToPrettyString());
        }

        [TestMethod]
        public void TestJson()
        {
            var ncGrammer = NcPGP.GetNcGrammerSource();
            var ncParser1 = NcParser.Load(ncGrammer);
            var ncParser2 = _P2P(ncParser1, ncGrammer);
            var jsonGrammer = Utils.ReadFromAssembly("Nccc.Tests.Json.json.grammer");
            var jsonParser0 = NcParser.Load(jsonGrammer);
            var jsonParser1 = _P2P(ncParser1, jsonGrammer);
            var jsonParser2 = _P2P(ncParser2, jsonGrammer);
            var jsonSample = Utils.ReadFromAssembly("Nccc.Tests.Json.sample.json");
            var pr0 = jsonParser0.ScanAndParse(jsonSample);
            var pr1 = jsonParser1.ScanAndParse(jsonSample);
            var pr2 = jsonParser2.ScanAndParse(jsonSample);
            Assert.IsTrue(pr0.IsSuccess());
            Assert.IsTrue(pr1.IsSuccess());
            Assert.IsTrue(pr2.IsSuccess());
            Assert.AreEqual(pr0.ToSExp().ToPrettyString(), pr1.ToSExp().ToPrettyString());
            Assert.AreEqual(pr0.ToSExp().ToPrettyString(), pr1.ToSExp().ToPrettyString());
        }
    }
}
