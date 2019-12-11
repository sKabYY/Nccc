using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nccc.Common;
using Nccc.Exceptions;
using Nccc.Scanner;

namespace Nccc.Parser
{
    public class CombinedParser
    {
        protected Locale _ { get; }
        protected CharScanner Scanner { get; }

        protected bool LeftRecurDetection { get; set; } = true;
        protected bool UseMemorizedParser { get; set; } = true;
        protected string MessageLocaleStart { get; set; } = null;
        protected string MessageLocaleEnd { get; set; } = null;

        protected IParser SpacingParser { private get; set; } = null;

        private IParser RootParser { get; set; }
        private readonly IDictionary<string, IParser> _env = new Dictionary<string, IParser>();

        protected CombinedParser()
        {
            _ = new Locale();
            Scanner = new CharScanner();
        }

        protected void SetRootParser(IParser parser)
        {
            RootParser = parser;
        }

        public ParseResult Parse(string src)
        {
            if (RootParser == null)
            {
                throw new ParseException("RootParser is uninitialized");
            }
            return ParseBy(RootParser, src);
        }

        public ParseResult ParseBy(string parser, string src)
        {
            return ParseBy(Get(parser), src);
        }

        private ParseResult ParseBy(IParser parser, string src)
        {
            var toks = Scanner.Scan(src);
            ResetMemorizedParsers();
            return WrapperEof(parser).Parse(toks, LeftRecurDetection ? ParseStack.Empty : FakeParseStack.Empty);
        }

        private IParser WrapperEof(IParser parser)
        {
            return CSeq(PSpacing(), parser, PEof());
        }

        private readonly IList<MemorizedParser> _memorizedParsers = new List<MemorizedParser>();
        private MemorizedParser MakeMemorizedParser(IParser parser)
        {
            var memorizedParser = new MemorizedParser(parser);
            _memorizedParsers.Add(memorizedParser);
            return memorizedParser;
        }
        private void ResetMemorizedParsers()
        {
            foreach (var p in _memorizedParsers)
            {
                p.Clear();
            }
        }

        protected IParser DefParser(string name, IParser parser)
        {
            if (UseMemorizedParser)
            {
                parser = MakeMemorizedParser(parser);
            }
            EnvSet(name, parser);
            return Get(name);
        }

        protected IParser Get(string name)
        {
            return new ParserImpl(name, (toks, stk) =>
            {
                var parser = EnvGet(name);
                return parser.Parse(toks, stk);
            });
        }

        #region builtin parsers

        protected IParser PEof()
        {
            return CIfFail("expect EOF", CNot(PAny()));
        }

        protected IParser PAlpha()
        {
            return CIfFail($"{_.L("expect")} alpha", COr(PRange('a', 'z'), PRange('A', 'Z')));
        }

        protected IParser PDigit()
        {
            return PRange('0', '9');
        }

        protected IParser PSpace()
        {
            return PTokenPred($"{_.L("expect")} whitespace", tok => char.IsWhiteSpace(tok.Value));
        }

        protected IParser PNewline()
        {
            return COr(PCharArray("\r\n"), PChar('\n'), PChar('\r'));
        }

        protected IParser PNumber()
        {
            var digits = CPlus(PDigit());
            return CSeq(
                CMaybe(COr(PChar('+'), PChar('-'))),  /* 符号 */
                digits,                               /* 整数部分 */
                CMaybe(CSeq(PChar('.'), digits)),     /* 小数部分 */
                CMaybe(                               /* 指数部分 */
                    COr(PChar('E'), PChar('e')),
                    CMaybe(PChar('-')),
                    digits),
                PSpacing());
        }

        protected void LoadBuildinParsers()
        {
            DefParser("alpha", PAlpha());
            DefParser("digit", PDigit());
            DefParser("number", PNumber());
        }

        #endregion

        #region char

        protected IParser PTokenPred(string failMessage, Func<Token, bool> pred)
        {
            return new ParserImpl((toks, stk) =>
            {
                if (toks.IsEof())
                {
                    return OutputFail(failMessage, toks);
                }
                var tok = toks.Car();
                if (pred(tok))
                {
                    return OutputNode(Node.MakeLeaf(tok), toks.Cdr());
                }
                else
                {
                    return OutputFail(failMessage, toks);
                }
            });
        }

        protected IParser PChar(char c, bool caseSensitive = true)
        {
            return PTokenPred($"{_.L("expect")} \"{c}\"", tok =>
            {
                if (caseSensitive)
                {
                    return tok.Value == c;
                }
                else
                {
                    return char.ToLower(tok.Value) == char.ToLower(c);
                }
            });
        }

        protected IParser PCharArray(string s, bool caseSensitive = true)
        {
            return CIfFail($"{_.L("expect")} \"{s}\"", CSeq(s.Select(c => PChar(c, caseSensitive)).ToArray()));
        }

        protected IParser PRange(char c1, char c2)
        {
            // TODO: locale
            return PTokenPred($"expect char between '{c1}' and '{c2}'", tok =>
            {
                return c1 <= tok.Value && tok.Value <= c2;
            });
        }

        protected IParser PEmpty()
        {
            return new ParserImpl((toks, stk) =>
            {
                return OutputEmpty(toks, toks.Position(), toks.Position());
            });
        }

        protected IParser PAny()
        {
            return new ParserImpl((toks, stk) =>
            {
                if (toks.IsEof())
                {
                    return OutputFail($"ANY fail: {_.L("reach eof")}", toks);
                }
                return OutputNode(Node.MakeLeaf(toks.Car()), toks.Cdr());
            });
        }

        protected IParser PSpacing()
        {
            return new ParserImpl((toks, stk) =>
            {
                if (SpacingParser == null) return OutputEmpty(toks, toks.Position(), toks.Position());
                return SpacingParser.Parse(toks, stk);
            });
        }

        #endregion

        #region token

        protected IParser PEqAndRetain(string s, bool caseSensitive = true)
        {
            return CSeq(PCharArray(s, caseSensitive), PSpacing());
        }

        protected IParser PEq(string s, bool caseSensitive = true)
        {
            return CGlob(PEqAndRetain(s, caseSensitive));
        }

        #endregion

        protected IParser CSeq(params IParser[] ps)
        {
            // TODO: 使用SeqParserImpl简化parser复杂度
            if (ps.Length == 0) throw new ParseException($"empty CSeq");
            if (ps.Length == 1) return ps.First();
            return new ParserImpl((toks, stk) =>
            {
                ParseResult failResult = null;
                ParseResult deepest = null;
                var results = new List<ParseResult>();
                foreach (var p in ps)
                {
                    var r = p.Parse(toks, stk);
                    if (r.Message != null)
                    {
                        deepest = r.Deeper(deepest);
                    }
                    if (!r.IsSuccess())
                    {
                        failResult = deepest.FailResult();
                        results.Add(failResult);
                        break;
                    }
                    toks = r.Rest;
                    results.Add(r);
                }
                var message = deepest?.Message;
                var failRest = deepest?.FailRest;
                return MergeResults(results, toks, message, failRest);
            });
        }

        protected IParser CDebug(params IParser[] ps)
        {
            var parser = CSeq(ps);
            return new ParserImpl((toks, stk) =>
            {
                var r = parser.Parse(toks, stk);
                Console.WriteLine($"===[Debug]===\n{r.ToSExp().ToPrettyString()}\n");
                return r;
            });
        }

        protected IParser CIs(string type, params IParser[] ps)
        {
            var parser = CSeq(ps);
            return new ParserImpl((toks, stk) =>
            {
                var r = parser.Parse(toks, stk);
                if (!r.IsSuccess())
                {
                    return r;
                }
                return OutputNode(Node.MakeNode(type, r.Nodes, r.Start, r.End), r.Rest, r.Message, r.FailRest);
            });
        }

        protected IParser CGlob(params IParser[] ps)
        {
            var parser = CSeq(ps);
            return new ParserImpl((toks, stk) =>
            {
                var r = parser.Parse(toks, stk);
                if (r.IsSuccess())
                {
                    return OutputEmpty(r.Rest, r.Start, r.End, r.Message, r.FailRest);
                }
                else
                {
                    return r;
                }
            });
        }

        protected IParser CIfFail(string failMessage, IParser parser)
        {
            return new ParserImpl((toks, stk) =>
            {
                var r = parser.Parse(toks, stk);
                if (r.IsSuccess())
                {
                    return r;
                }
                else
                {
                    return OutputFail(failMessage, r.Rest);
                }
            });
        }

        protected IParser COr(params IParser[] ps)
        {
            return new ParserImpl((toks, stk) =>
            {
                ParseResult result = null;
                ParseResult deepest = null;
                foreach (var p in ps)
                {
                    var r = p.Parse(toks, stk);
                    if (r.IsSuccess())
                    {
                        result = r;
                        break;
                    }
                    deepest = r.Deeper(deepest);
                }
                return result ?? deepest ?? OutputFail("empty OR", toks);
            });
        }

        protected IParser CNot(params IParser[] ps)
        {
            var parser = CSeq(ps);
            return new ParserImpl((toks, stk) =>
            {
                if (!toks.IsEof())
                {
                    var r = parser.Parse(toks, stk);
                    if (r.IsSuccess())
                    {
                        return OutputFail("NOT fail", toks);
                    }
                }
                return OutputEmpty(toks, toks.Position(), toks.Position());
            });
        }

        protected IParser CStar(params IParser[] ps)
        {
            var parser = CSeq(ps);
            return new ParserImpl((toks, stk) =>
            {
                var results = new List<ParseResult>();
                ParseResult deepest = null;
                while (true)
                {
                    var r = parser.Parse(toks, stk);
                    deepest = r.Deeper(deepest);
                    if (!r.IsSuccess())
                    {
                        return MergeResults(results, toks, deepest.Message, deepest.FailRest);
                    }
                    if (toks == r.Rest)
                    {
                        Fatal("empty-star detected", parser, toks, stk);
                    }
                    toks = r.Rest;
                    results.Add(r);
                }
            });
        }

        protected IParser CMaybe(params IParser[] ps)
        {
            return COr(CSeq(ps), PEmpty());
        }

        protected IParser CPlus(params IParser[] ps)
        {
            var parser = CSeq(ps);
            return CSeq(parser, CStar(parser));
        }

        protected IParser CJoin(IParser sepParser, params IParser[] ps)
        {
            var parser = CSeq(ps);
            return CMaybe(CJoinPlus(sepParser, parser));
        }

        protected IParser CJoinPlus(IParser sepParser, params IParser[] ps)
        {
            var parser = CSeq(ps);
            return CSeq(parser, CStar(sepParser, parser));
        }

        protected IParser CArray(IParser parser, int n)
        {
            var ps = Enumerable.Repeat(parser, n).ToArray();
            return CSeq(ps);
        }

        private IParser EnvGet(string name)
        {
            if (_env.TryGetValue(name, out IParser p))
            {
                return p;
            }
            throw new ParseException($"\"{name}\" {_.L("is undefined")}");
        }

        private void EnvSet(string name, IParser p)
        {
            _env[name] = p;
        }

        private ParseResult OutputNodes(IList<Node> nodes, TokenStream rest,
            TextPosition start, TextPosition end, string message = null, TokenStream failRest= null)
        {
            return OutputNodes(true, nodes, rest, start, end, message, failRest);
        }

        private ParseResult OutputNodes(bool success, IList<Node> nodes, TokenStream rest,
            TextPosition start, TextPosition end, string message = null, TokenStream failRest= null)
        {
            return new ParseResult
            {
                Success = success,
                Nodes = nodes,
                Rest = rest,
                Message = message,
                FailRest = failRest ?? rest,
                Start = start,
                End = end
            };
        }

        private ParseResult OutputNode(Node node, TokenStream rest,
            string message = null, TokenStream failRest = null)
        {
            return OutputNodes(new List<Node> { node }, rest, node.Start, node.End, message, failRest);
        }

        private ParseResult OutputEmpty(TokenStream rest, TextPosition start, TextPosition end,
            string message = null, TokenStream failRest = null)
        {
            return OutputNodes(new List<Node>(), rest, start, end, message, failRest);
        }

        private ParseResult OutputFail(string message, TokenStream rest)
        {
            return new ParseResult
            {
                Success = false,
                Message = MessageLocaleString(message),
                Rest = rest,
                FailRest = rest
            };
        }

        // Assert results[0...-1] succeed
        private ParseResult MergeResults(IList<ParseResult> results, TokenStream rest, string message, TokenStream failRest)
        {
            var nodes = results.Where(r => r.IsSuccess()).SelectMany(r => r.Nodes).ToList();
            var success = true;
            if (results.Count >= 1)
            {
                var lastR = results.Last();
                if (message == null)
                {
                    message = lastR.Message;
                    failRest = lastR.FailRest;
                }
                success = lastR.IsSuccess();
            }
            var startIdx = results.Count == 0 ? rest.Position() : results.First().Start;
            var endIdx = results.Count == 0 ? rest.Position() : results.Last().End;
            return OutputNodes(success, nodes, rest, startIdx, endIdx, message, failRest);
        }

        public static void Fatal(string message, IParser parser, TokenStream toks, IParseStack stk)
        {
            throw new ParseException($"{message}\n" +
                $"parser: {parser.ToString()}\n" +
                $"rest: {toks.ToString()}\n" +
                $"stack trace: {stk.ToSExp().ToPrettyString()}");
        }

        private string MessageLocaleString(string s)
        {
            if (string.IsNullOrEmpty(MessageLocaleStart) || string.IsNullOrEmpty(MessageLocaleEnd))
            {
                return s;
            }
            var pattern = $"{MessageLocaleStart}([^{MessageLocaleEnd}])*{MessageLocaleEnd}";
            var mc = Regex.Matches(s, pattern);
            var items = mc.Cast<Match>().Select(m => m.Value).Distinct().ToList();
            foreach (var item in items)
            {
                var key = item.Substring(MessageLocaleStart.Length, item.Length - MessageLocaleStart.Length - MessageLocaleEnd.Length);
                s = s.Replace(item, _.L(key));
            }
            return s;
        }
    }
}
