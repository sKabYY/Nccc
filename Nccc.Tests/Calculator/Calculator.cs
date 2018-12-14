using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests.Calculator
{
    [TestClass]
    public class Calculator
    {
        private NcParser _parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Calculator.calculator.grammer");

        [TestMethod]
        public void TestParser()
        {
            var pr = _parser.ScanAndParse("(5.1+2)*3+-2^3^2");
            Console.WriteLine(pr.ToSExp().ToPrettyString());
        }

        [TestMethod]
        public void Test()
        {
            var x = _Calc("(5.1+2)*3+-2^3^2");
            var x0 = (5.1 + 2) * 3 - Math.Pow(2, Math.Pow(3, 2));
            Console.WriteLine(x);
            Assert.AreEqual(x0, x);
        }

        private double _Calc(string exp)
        {
            var pr = _parser.ScanAndParse(exp);
            if (!pr.IsSuccess())
            {
                throw new ArgumentException($"Parsing fail: {pr.ToSExp().ToPrettyString()}");
            }
            return _ValueOf(pr.Nodes.First());
        }

        private double _ValueOf(Node node)
        {
            return node.Match<double>(type =>
            {
                type("par", es => _ValueOf(es.First()));
                type("add", es => _ValueOf(es[0]) + _ValueOf(es[1]));
                type("sub", es => _ValueOf(es[0]) - _ValueOf(es[1]));
                type("mul", es => _ValueOf(es[0]) * _ValueOf(es[1]));
                type("div", es => _ValueOf(es[0]) / _ValueOf(es[1]));
                type("pow", es => Math.Pow(_ValueOf(es[0]), _ValueOf(es[1])));
                type("neg", es => -_ValueOf(es.First()));
                type("num", es => double.Parse(es.First().Value));
            });
        }
    }
}
