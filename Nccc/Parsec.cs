using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nccc
{
    public class Parsec
    {
        public Scanner Scanner { get; set; } = new Scanner();
        protected IParser RootParser { get; set; }
        private readonly IDictionary<string, IParser> _env = new Dictionary<string, IParser>();
        public bool LeftRecurDetection { get; set; } = true;
        public bool UseMemorizedParser { get; set; } = true;

        public ParseResult ScanAndParse(string src)
        {
            var toks = Scanner.Scan(src).FilterComment();
            if (RootParser == null)
            {
                throw new ParseException("RootParser is uninitialized");
            }
            _ResetMemorizedParsers();
            var r = RootParser.Parse(toks, LeftRecurDetection ? ParseStack.Empty : FakeParseStack.Empty);
            if (r.Rest.IsEof())
            {
                return r;
            }
            else
            {
                if (r.Message == null)
                {
                    return new ParseResult
                    {
                        Message = "expect <<EOF>>",
                        Rest = r.Rest,
                        FailRest = r.FailRest
                    };
                }
                else
                {
                    return r.FailResult();
                }
            }
        }

        private IList<MemorizedParser> _memorizedParsers = new List<MemorizedParser>();
        private MemorizedParser _MakeMemorizedParser(IParser parser)
        {
            var memorizedParser = new MemorizedParser(parser);
            _memorizedParsers.Add(memorizedParser);
            return memorizedParser;
        }
        private void _ResetMemorizedParsers()
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
                parser = _MakeMemorizedParser(parser);
            }
            _EnvSet(name, parser);
            return parser;
        }

        protected IParser Get(string name)
        {
            return new ParserImpl(name, (toks, stk) =>
            {
                var parser = _EnvGet(name);
                return parser.Parse(toks, stk);
            });
        }

        protected IParser CSeq(params IParser[] ps)
        {
            // TODO: 使用SeqParserImpl简化parser复杂度
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
                        break;
                    }
                    toks = r.Rest;
                    results.Add(r);
                }
                var message = deepest?.Message;
                var failRest = deepest?.FailRest;
                return failResult ?? _MergeResults(results, toks, message, failRest);
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
                return _OutputNode(Node.MakeNode(type, r.Nodes, r.Start, r.End), r.Rest, r.Message, r.FailRest);
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
                    return _OutputEmpty(r.Rest, r.Start, r.End, r.Message, r.FailRest);
                }
                else
                {
                    return r;
                }
            });
        }

        protected IParser PTokenPred(string failMessage, Func<Token, bool> pred)
        {
            return new ParserImpl((toks, stk) =>
            {
                var tok = toks.Car();
                if (tok.IsEof())
                {
                    return _OutputFail(failMessage, toks);
                }
                if (pred(tok))
                {
                    return _OutputNode(Node.MakeLeaf(tok), toks.Cdr());
                }
                else
                {
                    return _OutputFail(failMessage, toks);
                }
            });
        }

        protected IParser PEq(string s, bool caseSensitive = true)
        {
            var p = PTokenPred($"expect \"{s}\"", tok =>
            {
                if (!tok.IsToken()) return false;
                if (caseSensitive)
                {
                    return tok.Text == s;
                }
                else
                {
                    return tok.Text.ToLower() == s.ToLower();
                }
            });
            return CGlob(p);
        }

        protected IParser PRegex(string pattern, string failMessage = null)
        {
            var regex = new Regex(pattern);
            return PTokenPred(failMessage ?? $"not match regex /{pattern}/", tok =>
            {
                if (!tok.IsToken()) return false;
                return regex.IsMatch(tok.Text);
            });
        }

        protected IParser PTokenType(TokenType type)
        {
            return PTokenType(type.ToString().ToLower());
        }

        protected IParser PTokenType(string type)
        {
            return PTokenPred($"expect <{type}>", tok =>
            {
                return tok.Type.ToString().ToLower() == type;
            });
        }

        protected IParser PEmpty()
        {
            return new ParserImpl((toks, stk) =>
            {
                return _OutputEmpty(toks, toks.Position(), toks.Position());
            });
        }

        protected IParser PAny()
        {
            return new ParserImpl((toks, stk) =>
            {
                if (toks.IsEof())
                {
                    return _OutputFail("ANY fail: reach eof", toks);
                }
                return _OutputNode(Node.MakeLeaf(toks.Car()), toks.Cdr());
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
                    return _OutputFail(failMessage, r.Rest);
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
                return result ?? deepest ?? _OutputFail("empty OR", toks);
            });
        }

        protected IParser CNot(params IParser[] ps)
        {
            var parser = CSeq(ps);
            return new ParserImpl((toks, stk) =>
            {
                if (toks.IsEof())
                {
                    return _OutputFail("NOT fail: reach eof", toks);
                }
                var r = parser.Parse(toks, stk);
                if (r.IsSuccess())
                {
                    return _OutputFail("NOT fail", toks);
                }
                return _OutputEmpty(toks, toks.Position(), toks.Position());
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
                        return _MergeResults(results, toks, deepest.Message, deepest.FailRest);
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

        private IParser _EnvGet(string name)
        {
            if (_env.TryGetValue(name, out IParser p))
            {
                return p;
            }
            throw new ParseException($"\"{name}\" is undefined");
        }

        private void _EnvSet(string name, IParser p)
        {
            _env[name] = p;
        }

        private ParseResult _OutputNodes(IList<Node> nodes, BaseStream<Token> rest,
            TextPosition start, TextPosition end, string message = null, BaseStream<Token> failRest= null)
        {
            return new ParseResult
            {
                Nodes = nodes,
                Rest = rest,
                Message = message,
                FailRest = failRest ?? rest,
                Start = start,
                End = end
            };
        }

        private ParseResult _OutputNode(Node node, BaseStream<Token> rest,
            string message = null, BaseStream<Token> failRest = null)
        {
            return _OutputNodes(new List<Node> { node }, rest, node.Start, node.End, message, failRest);
        }

        private ParseResult _OutputEmpty(BaseStream<Token> rest, TextPosition start, TextPosition end,
            string message = null, BaseStream<Token> failRest = null)
        {
            return _OutputNodes(new List<Node>(), rest, start, end, message, failRest);
        }

        private ParseResult _OutputFail(string message, BaseStream<Token> rest)
        {
            return new ParseResult
            {
                Message = message,
                Rest = rest,
                FailRest = rest
            };
        }

        private ParseResult _MergeResults(IList<ParseResult> results, BaseStream<Token> rest, string message, BaseStream<Token> failRest)
        {
            var nodes = results.SelectMany(r => r.Nodes).ToList();
            if (message == null && results.Count > 1)
            {
                var lastR = results.Last();
                message = lastR.Message;
                failRest = lastR.FailRest;
            }
            var startIdx = results.Count == 0 ? rest.Position() : results.First().Start;
            var endIdx = results.Count == 0 ? rest.Position() : results.Last().End;
            return _OutputNodes(nodes, rest, startIdx, endIdx, message, failRest);
        }

        public static void Fatal(string message, IParser parser, BaseStream<Token> toks, IParseStack stk)
        {
            throw new ParseException($"{message}\n" +
                $"parser: {parser.ToString()}\n" +
                $"rest: {toks.ToString()}\n" +
                $"stack trace: {stk.ToSExp().ToPrettyString()}");
        }
    }

    public interface IParser
    {
        ParseResult Parse(BaseStream<Token> toks, IParseStack stk);
    }

    public class ParserImpl : IParser
    {
        private readonly string _name;
        private readonly Func<BaseStream<Token>, IParseStack, ParseResult> _parse;
        public ParserImpl(string name, Func<BaseStream<Token>, IParseStack, ParseResult> parse)
        {
            _name = name;
            _parse = parse;
        }
        public ParserImpl(Func<BaseStream<Token>, IParseStack, ParseResult> parse): this(null, parse) { }
        public ParseResult Parse(BaseStream<Token> toks, IParseStack stk)
        {
            if (stk.Has(this, toks))
            {
                Parsec.Fatal("left-recursion detected", this, toks, stk);
            }
            return _parse(toks, stk.Extend(this, toks));
        }
        public override string ToString()
        {
            if (_name == null) return base.ToString();
            return $"<Parser:{_name}>";
        }
    }

    public class MemorizedParser : IParser
    {
        private readonly IDictionary<int, ParseResult> _memo = new Dictionary<int, ParseResult>();
        private readonly IParser _parser;
        public MemorizedParser(IParser parser)
        {
            _parser = parser;
        }
        public ParseResult Parse(BaseStream<Token> toks, IParseStack stk)
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

    public class ParseResult
    {
        public IList<Node> Nodes { get; set; }
        public BaseStream<Token> Rest { get; set; }
        public string Message { get; set; }
        public BaseStream<Token> FailRest { get; set; }
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }

        public bool IsSuccess()
        {
            return Nodes != null;
        }

        public ParseResult Deeper(ParseResult r)
        {
            if (r == null || r.FailRest.Position().Offset < FailRest.Position().Offset)
            {
                return this;
            }
            else
            {
                return r;
            }
        }

        public ParseResult FailResult()
        {
            return new ParseResult
            {
                Nodes = null,
                Rest = Rest,
                Message = Message,
                FailRest = FailRest,
                Start = Start,
                End = End
            };
        }

        public SExp ToSExp()
        {
            return SExp.List(
                SExp.List("success?", IsSuccess()),
                SExp.List("nodes", SExp.List(Nodes?.Select(n => n.ToSExp()).ToArray())),
                SExp.List("rest", Rest),
                SExp.List("message", Message),
                SExp.List("fail_rest", FailRest));
        }
    }

    public interface IParseStack
    {
        bool IsEmpty();
        bool Has(IParser parser, BaseStream<Token> toks);
        IParseStack Extend(IParser parser, BaseStream<Token> toks);
        ListSExp ToSExp();
    }

    public class ParseStack: IParseStack
    {
        private readonly IParser _parser;
        private readonly BaseStream<Token> _toks;
        private readonly IParseStack _prev;

        public static IParseStack Empty { get; } = new ParseStack(null, null, null);

        private ParseStack(IParser parser, BaseStream<Token> toks, IParseStack prev)
        {
            _parser = parser;
            _toks = toks;
            _prev = prev;
        }

        public bool IsEmpty()
        {
            return this == Empty;
        }

        public bool Has(IParser parser, BaseStream<Token> toks)
        {
            if (IsEmpty()) return false;
            if (parser == _parser && toks.Equals(_toks)) return true;
            return _prev.Has(parser, toks);
        }

        public IParseStack Extend(IParser parser, BaseStream<Token> toks)
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

    public class FakeParseStack : IParseStack
    {
        private FakeParseStack() { }
        public static IParseStack Empty { get; } = new FakeParseStack();

        public IParseStack Extend(IParser parser, BaseStream<Token> toks)
        {
            return this;
        }

        public bool Has(IParser parser, BaseStream<Token> toks)
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

    public class Node
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public IList<Node> Children { get; set; }
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }

        public bool IsLeaf()
        {
            return Children == null;
        }

        public String LeafValue()
        {
            if (Children == null || Children.Count != 1 || !Children.First().IsLeaf())
            {
                throw new ParseException("can't get LeafValue of ??");  // TODO
            }
            return Children.First().Value;
        }

        public static Node MakeLeaf(Token tok)
        {
            return new Node
            {
                Value = tok.Text,
                Start = tok.Start,
                End = tok.End
            };
        }

        public static Node MakeNode(string type, IList<Node> children, TextPosition start, TextPosition end)
        {
            return new Node
            {
                Type = type,
                Children = children,
                Start = start,
                End = end
            };
        }

        public SExp ToSExp()
        {
            if (IsLeaf())
            {
                return SExp.Value(Value);
            }
            else
            {
                var info = $"{Type}[({Start.Linenum},{Start.Colnum})-({End.Linenum},{End.Colnum})]";
                var lst = SExp.List(Children.Select(n => n.ToSExp()).ToArray());
                lst.PushFront(info);
                return lst;
            }
        }

        public static void Match(Node node, Action<Action<string, Action<IList<Node>>>> block)
        {
            var hit = false;
            block((type, proc) =>
            {
                if (node.Type == type)
                {
                    proc(node.Children);
                    hit = true;
                }
            });
            if (!hit) throw new Exception($"match error: unknown type: {node.Type}");
        }

        public static T Match<T>(Node node, Action<Action<string, Func<IList<Node>, T>>> block)
        {
            var value = default(T);
            var hit = false;
            block((type, proc) =>
            {
                if (node.Type == type)
                {
                    value = proc(node.Children);
                    hit = true;
                }
            });
            if (!hit) throw new Exception($"match error: unknown type: {node.Type}");
            return value;
        }
    }

    public class ParseException: Exception
    {
        public ParseException(string message) : base(message) { }
    }
}
