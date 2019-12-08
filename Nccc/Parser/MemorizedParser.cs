using Nccc.Scanner;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Parser
{
    class MemorizedParser : IParser
    {
        private readonly IDictionary<int, ParseResult> _memo = new Dictionary<int, ParseResult>();
        private readonly IParser _parser;
        public MemorizedParser(IParser parser)
        {
            _parser = parser;
        }
        public ParseResult Parse(TokenStream toks, IParseStack stk)
        {
            var offset = toks.Car().Start.Offset;
            if (!_memo.TryGetValue(offset, out ParseResult r))
            {
                r = _parser.Parse(toks, stk);
                _memo[offset] = r;
            }
            return r;
        }
        public void Clear()
        {
            _memo.Clear();
        }
        public override string ToString()
        {
            return _parser.ToString();
        }
    }
}
