using Nccc.Common;
using Nccc.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NcGP = Nccc.Parser.NcGrammerParser;

namespace Nccc.Parser
{
    public class NcParser: CombinedParser
    {
        private bool CaseSensitive;
        private bool SplitWord;

        public class Settings
        {
            private readonly NcParser _parser;
            public Settings(NcParser p)
            {
                _parser = p;
            }

            public Locale Locale => _parser._;

            public bool CaseSensitive
            {
                set => _parser.CaseSensitive = value;
            }

            public bool SplitWord
            {
                set => _parser.SplitWord = value;
            }
            public bool LeftRecurDetection
            {
                set => _parser.LeftRecurDetection = value;
            }
            public bool UseMemorizedParser
            {
                set => _parser.UseMemorizedParser = value;
            }
        }

        public NcParser(Node grammerAst, Action<Settings> init)
        {
            // default options
            CaseSensitive = true;
            SplitWord = true;
            LeftRecurDetection = true;
            UseMemorizedParser = true;
            SpacingParser = CGlob(CStar(PSpace()));
            // ===============

            var nodes = grammerAst.Children;
            var rootStm = nodes.First();
            var optionStms = nodes[1].Children;
            var defStms = nodes[2].Children;
            var rootName = rootStm.StringValue();

            Node.Match(optionStms, type =>
            {
                type(NcGP.OPTION_STM, es =>
                {
                    SetOption(es.First(), es.Skip(1).ToList());
                });
            });

            init?.Invoke(new Settings(this));

            Node.Match(defStms, type =>
            {
                type(NcGP.DEF_STM, es =>
                {
                    var name = es.First().StringValue();
                    var ps = ValueOf(es.Skip(1));
                    DefParser(name, CSeq(ps));
                });
            });

            SetRootParser(Get(rootName));
        }

        private void SetOption(Node cmd, IList<Node> args)
        {
            cmd.Match(type =>
            {
                type(NcGP.CASE_SENSITIVE, _ =>
                {
                    CaseSensitive = ValueOfOnOrOff(args.First());
                });
                type(NcGP.LEX_IGNORE, _ =>
                {
                    SpacingParser = Get(args.First().StringValue());
                });
                type(NcGP.LEX_MODE, _ =>
                {
                    SpacingParser = null;
                });
                type(NcGP.SPLIT_WORD, _ =>
                {
                    SplitWord = ValueOfOnOrOff(args.First());
                });
                type(NcGP.INCLUDE_BUILTIN, _ =>
                {
                    LoadBuildinParsers();
                });
                type(NcGP.SET_MESSAGE_LOCALE_START, _ =>
                {
                    MessageLocaleStart = args.First().StringValue();
                });
                type(NcGP.SET_MESSAGE_LOCALE_END, _ =>
                {
                    MessageLocaleEnd = args.First().StringValue();
                });
            });
        }

        private bool ValueOfOnOrOff(Node node)
        {
            return node.Match<bool>(type =>
            {
                type(NcGP.OPTION_ON, _ => true);
                type(NcGP.OPTION_OFF, _ => false);
            });
        }

        private IParser ValueOf(Node exp)
        {
            return exp.Match<IParser>(type =>
            {
                type(NcGP.NAMED_EXP, es => CIs(es.First().StringValue(), ValueOf(es[1])));
                type(NcGP.GLOB_EXP, es => CGlob(ValueOf(es.First())));
                type(NcGP.OP_EXP, es => ApplyOp(es.First(), ValueOf(es.Skip(1))));
                type(NcGP.ERR_EXP, es => CIfFail(es.First().StringValue(), CSeq(ValueOf(es.Skip(1)))));
                type(NcGP.SEQ_EXP, es => CSeq(ValueOf(es)));
                type(NcGP.ANY_EXP, es => PAny());
                type(NcGP.DBG_EXP, es => CDebug(CSeq(ValueOf(es))));
                type(NcGP.DBG_1EXP, es => CDebug(ValueOf(es.First())));
                type(NcGP.CHAR_EXP, es => ValueOfCharExp(es.First()));
                type(NcGP.GLOB_CHAR_EXP, es => CGlob(ValueOfCharExp(es.First())));
                type(NcGP.WORD_EXP, es => ValueOfWordExp(es));
                type(NcGP.GLOB_WORD_EXP, es => CGlob(ValueOfWordExp(es)));
                type(NcGP.VAR_EXP, es => Get(Node.ConcatValue(es)));
                type(NcGP.ARRAY_EXP, es =>
                {
                    var parser = ValueOf(es[1]);
                    if (int.TryParse(es[0].StringValue(), out int size))
                    {
                        return CArray(parser, size);
                    }
                    var pos = es[0].Start;
                    throw new ParseException($"expect an integer at row {pos.Linenum} column {pos.Colnum}");
                });
            });
        }

        private IParser[] ValueOf(IEnumerable<Node> es)
        {
            return es.Select(ValueOf).ToArray();
        }

        private IParser ValueOfCharExp(Node node)
        {
            return node.Match<IParser>(type =>
            {
                type(NcGP.NORMAL_CHAR, es =>
                {
                    if (es.Count == 1)
                    {
                        return PChar(es.First().Value);
                    }
                    var pos = es.First().Start;
                    throw new ParseException($"not a char (row {pos.Linenum}, column {pos.Colnum})");
                });
                type(NcGP.SPECIAL_CHAR, es =>
                {
                    var charName = Node.ConcatValue(es);
                    switch (charName)
                    {
                        case NcGP.SPECIAL_CHAR_EOF: return PEof();
                        case NcGP.SPECIAL_CHAR_NEWLINE: return PNewline();
                        case NcGP.SPECIAL_CHAR_SPACE: return PSpace();
                        default:
                            var pos = es.First().Start;
                            throw new ParseException($"unknown char '{charName}' at row {pos.Linenum} column {pos.Colnum}");
                    }
                });
            });
        }

        private IParser ValueOfWordExp(IList<Node> es)
        {
            var word = Node.ConcatValue(es);
            if (!string.IsNullOrWhiteSpace(word) && SplitWord)
            {
                return CSeq(word.Split()
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(w => PEqAndRetain(w, CaseSensitive))
                    .ToArray());
            }
            else
            {
                return PEqAndRetain(word, CaseSensitive);
            }
        }

        private IParser ApplyOp(Node op, params IParser[] ps)
        {
            var opName = op.StringValue();
            switch (opName)
            {
                case NcGP.PLUS_CMB: return CPlus(ps);
                case NcGP.STAR_CMB: return CStar(ps);
                case NcGP.OR_CMB: return COr(ps);
                case NcGP.SEQ_CMB: return CSeq(ps);
                case NcGP.JOIN_CMB: return CJoin(ps.First(), ps.Skip(1).ToArray());
                case NcGP.JOIN_PLUS_CMB: return CJoinPlus(ps.First(), ps.Skip(1).ToArray());
                case NcGP.NOT_CMB: return CNot(ps);
                case NcGP.MAYBE_CMB: return CMaybe(ps);
                default:
                    var pos = op.Start;
                    throw new ParseException($"unkown op '{opName}' at row {pos.Linenum} column {pos.Colnum}");
            }
        }

        public static NcParser Load(string src, Action<Settings> init = null)
        {
            var ncgp = new NcGP();
            var parseResult = ncgp.Parse(src);
            if (!parseResult.IsSuccess())
            {
                throw new ParseException($"parsing grammer failed: (message: {parseResult.Message}, rest: {parseResult.FailRest.ToString()})");
            }
            return Load(parseResult.Nodes.First(), init);
        }

        public static NcParser Load(Node grammerAst, Action<Settings> init = null)
        {
            return new NcParser(grammerAst, init);
        }

        public static NcParser LoadFromAssembly(Assembly assembly, string path, Action<Settings> init = null)
        {
            var src = assembly.ReadString(path);
            return Load(src, init);
        }

    }
}
