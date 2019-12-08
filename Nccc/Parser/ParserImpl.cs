using Nccc.Scanner;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Parser
{
    class ParserImpl : IParser
    {
        private readonly string _name;
        private readonly Func<TokenStream, IParseStack, ParseResult> _parse;
        public ParserImpl(string name, Func<TokenStream, IParseStack, ParseResult> parse)
        {
            _name = name;
            _parse = parse;
        }
        public ParserImpl(Func<TokenStream, IParseStack, ParseResult> parse): this(null, parse) { }
        public ParseResult Parse(TokenStream toks, IParseStack stk)
        {
            if (stk.Has(this, toks))
            {
                this.Fatal("left-recursion detected", this, toks, stk);
            }
            var result = _parse(toks, stk.Extend(this, toks));
            result.ParserName = _name;
            return result;
        }
        public override string ToString()
        {
            if (_name == null) return base.ToString();
            return $"<Parser:{_name}>";
        }
    }

}
