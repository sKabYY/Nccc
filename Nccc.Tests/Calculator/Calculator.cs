using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nccc.Parser;
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
        private readonly NcParser _parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Calculator.calculator.grammer");

        [TestMethod]
        public void TestParser()
        {
            var pr = _parser.Parse("(5.1+2)*3+-2^3^2");
            Console.WriteLine(pr.ToSExp().ToPrettyString());
        }

        private double Calc(string exp)
        {
            var pr = _parser.Parse(exp);
            if (!pr.IsSuccess())
            {
                throw new ArgumentException($"Parsing fail: {pr.ToSExp().ToPrettyString()}");
            }
            return ValueOf(pr.Nodes.First());
        }

        private double ValueOf(Node node)
        {
            return node.Match<double>(type =>
            {
                type("par", es => ValueOf(es[0]));
                type("add", es => ValueOf(es[0]) + ValueOf(es[1]));
                type("sub", es => ValueOf(es[0]) - ValueOf(es[1]));
                type("mul", es => ValueOf(es[0]) * ValueOf(es[1]));
                type("div", es => ValueOf(es[0]) / ValueOf(es[1]));
                type("pow", es => Math.Pow(ValueOf(es[0]), ValueOf(es[1])));
                type("neg", es => -ValueOf(es[0]));
                type("num", es => double.Parse(Node.ConcatValue(es)));
            });
        }

        [TestMethod]
        public void TestCalc()
        {
            var x = Calc("(5.1 +2)* 3+-2 ^3^2");
            var x0 = (5.1 + 2) * 3 - Math.Pow(2, Math.Pow(3, 2));
            Console.WriteLine(x);
            Assert.AreEqual(x0, x);
        }

        [TestMethod]
        public void TestDig()
        {
            var pr = _parser.Parse("(5.1+2)*3+-2^3^2");
            Console.WriteLine(pr);
            var node = Node.DigNode(pr.Nodes, "add", "mul");
            Assert.AreEqual("mul", node.Type);
            var value = Node.DigValue(pr.Nodes, "add", "neg", "pow", "num");
            Assert.AreEqual("2", value);
        }
    }
}
