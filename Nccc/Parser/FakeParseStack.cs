using Nccc.Common;
using Nccc.Scanner;

namespace Nccc.Parser
{
    class FakeParseStack : IParseStack
    {
        private FakeParseStack() { }
        public static IParseStack Empty { get; } = new FakeParseStack();

        public IParseStack Extend(IParser parser, TokenStream toks)
        {
            return this;
        }

        public bool Has(IParser parser, TokenStream toks)
        {
            return false;
        }

        public bool IsEmpty()
        {
            return true;
        }

        public ListSExp ToSExp()
        {
            return SExp.List();
        }
    }
}
