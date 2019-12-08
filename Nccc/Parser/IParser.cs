using Nccc.Exceptions;
using Nccc.Scanner;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Parser
{
    public interface IParser
    {
        ParseResult Parse(TokenStream toks, IParseStack stk);
    }

    static class IParserExtension
    {
        public static void Fatal(this IParser _, string message, IParser parser, TokenStream toks, IParseStack stk)
        {
            throw new ParseException($"{message}\n" +
                $"parser: {parser.ToString()}\n" +
                $"rest: {toks.ToString()}\n" +
                $"stack trace: {stk.ToSExp().ToPrettyString()}");
        }

    }
}
