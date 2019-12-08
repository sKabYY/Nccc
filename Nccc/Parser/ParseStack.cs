using Nccc.Common;
using Nccc.Scanner;
using System;

namespace Nccc.Parser
{
    class ParseStack : IParseStack
    {
        private readonly IParser _parser;
        private readonly TokenStream _toks;
        private readonly IParseStack _prev;

        public static IParseStack Empty { get; } = new ParseStack(null, null, null);

        private ParseStack(IParser parser, TokenStream toks, IParseStack prev)
        {
            _parser = parser;
            _toks = toks;
            _prev = prev;
        }

        public bool IsEmpty()
        {
            return this == Empty;
        }

        public bool Has(IParser parser, TokenStream toks)
        {
            if (IsEmpty()) return false;
            if (parser == _parser && toks.Equals(_toks)) return true;
            return _prev.Has(parser, toks);
        }

        public IParseStack Extend(IParser parser, TokenStream toks)
        {
            return new ParseStack(parser, toks, this);
        }

        public ListSExp ToSExp()
        {
            if (!IsEmpty())
            {
                var lst = SExp.List(SExp.List(
                    SExp.List("parser", _parser.ToString()),
                    SExp.List("toks", _toks.ToString())));
                lst.Append(_prev.ToSExp());
                return lst;
            }
            else
            {
                return SExp.List();
            }
        }
    }
}
