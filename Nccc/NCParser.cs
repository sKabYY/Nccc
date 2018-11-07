using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc
{

    public class NcPGP: Parsec  // short for NcParserGeneratorParser
    {
        public const string PLUS_CMB = "plus-cmb";
        public const string STAR_CMB = "star-cmb";
        public const string JOIN_CMB = "join-cmb";
        public const string JOIN_PLUS_CMB = "join-plus-cmb";
        public const string OR_CMB = "or-cmb";
        public const string SEQ_CMB = "seq-cmb";
        public const string NOT_CMB = "not-cmb";
        public const string MAYBE_CMB = "maybe-cmb";

        public const string EXP = "exp";
        public const string NAMED_EXP = "named-exp";
        public const string GLOB_EXP = "glob-exp";
        public const string OP_EXP = "op-exp";
        public const string SEQ_EXP = "seq-exp";
        public const string TOKEN_TYPE_EXP = "token-type-exp";
        public const string ANY_EXP = "any-exp";
        public const string DBG_EXP = "dbg-exp";
        public const string DBG_1EXP = "dbg-1exp";
        public const string WORD_EXP = "word-exp";
        public const string REGEX_EXP = "regex-exp";
        public const string VAR_EXP = "var-exp";

        public const string DEF_STM = "def-stm";
        public const string DEF_ROOT = "def-root";
        public const string PROGRAM = "program";

        public NcPGP()
        {
            Scanner.Delims = new string[] { "(", ")", "[", "]", "{", "}", "<", ">", "`", "::", "=", ":", "~", "??" };
            Scanner.QuotationMarks = new string[] { "'" };
            Scanner.RegexMarks = new string[] { "/" };
            Scanner.CommentStart = "#|";
            Scanner.CommentEnd = "|#";

            var variablePattern = @"[a-zA-Z_\-][a-zA-Z0-9_\-]*";

            var lparen = PEq("(");
            var rparen = PEq(")");
            var eq = PEq("=");
            var word = PTokenType(TokenType.Str);
            var regex = PTokenType(TokenType.Regex);
            var variable = CIfFail("invalid var", PRegex(variablePattern));
            var cmb_op = CIfFail("unknown op", COr(
                CIs(PLUS_CMB, PEq("@+")),
                CIs(STAR_CMB, PEq("@*")),
                CIs(JOIN_CMB, PEq("@,*")),
                CIs(JOIN_PLUS_CMB, PEq("@,+")),
                CIs(OR_CMB, PEq("@or")),
                CIs(SEQ_CMB, PEq("@..")),
                CIs(NOT_CMB, PEq("@!")),
                CIs(MAYBE_CMB, PEq("@?"))));

            var named_exp = CSeq(variable, PEq(":"), Get(EXP));
            var glob_exp = CSeq(PEq("~"), Get(EXP));
            var op_exp = CSeq(lparen, cmb_op, CStar(Get(EXP)), rparen);
            var seq_exp = CSeq(lparen, CPlus(Get(EXP)), rparen);
            var dbg_exp = CSeq(PEq("["), CPlus(Get(EXP)), PEq("]"));
            var any_exp = CSeq(PEq("<"), PEq("*"), PEq(">"));
            var token_type_exp = CSeq(PEq("<"), PRegex(variablePattern), PEq(">"));
            var dbg_1exp = CSeq(PEq("??"), Get(EXP));
            var exp = DefParser(EXP, COr(
                CIs(NAMED_EXP, named_exp),
                CIs(GLOB_EXP, glob_exp),
                CIs(OP_EXP, op_exp),
                CIs(SEQ_EXP, seq_exp),
                CIs(ANY_EXP, any_exp),
                CIs(TOKEN_TYPE_EXP, token_type_exp),
                CIs(DBG_EXP, dbg_exp),
                CIs(DBG_1EXP, dbg_1exp),
                CIs(WORD_EXP, word),
                CIs(REGEX_EXP, regex),
                CIs(VAR_EXP, CSeq(variable, CNot(PEq(":"))))));

            var def_stm = CIs(DEF_STM, CSeq(variable, eq, CPlus(exp, CNot(eq))));
            var root_stm = CIs(DEF_ROOT, CSeq(PEq("::"), variable));

            RootParser = CIs(PROGRAM, CSeq(root_stm, CStar(def_stm)));
        }
    }
    public class NcParser: Parsec
    {
        public bool CaseSensitive { get; set; }
        public bool SplitWord { get; set; }

        private NcParser(Node grammerAst, Action<NcParser> init)
        {
            CaseSensitive = true;
            SplitWord = true;
            init?.Invoke(this);

            var nodes = grammerAst.Children;
            var rootStm = nodes.First();
            var defStms = nodes.Skip(1).ToList();
            var rootName = rootStm.LeafValue();

            foreach (var defStm in defStms)
            {
                Node.Match(defStm, (type) =>
                {
                    type(NcPGP.DEF_STM, es =>
                    {
                        var name = es.First().Value;
                        var ps = _ValueOf(es.Skip(1));
                        DefParser(name, CSeq(ps));
                    });
                });
            }
            RootParser = Get(rootName);
        }

        private IParser _ValueOf(Node exp)
        {
            return Node.Match<IParser>(exp, (type) =>
            {
                type(NcPGP.NAMED_EXP, es => CIs(es.First().Value, _ValueOf(es[1])));
                type(NcPGP.GLOB_EXP, es => CGlob(_ValueOf(es.First())));
                type(NcPGP.OP_EXP, es => _ApplyOp(es.First(), _ValueOf(es.Skip(1))));
                type(NcPGP.SEQ_EXP, es => CSeq(_ValueOf(es)));
                type(NcPGP.ANY_EXP, es => PAny());
                type(NcPGP.TOKEN_TYPE_EXP, es => PTokenType(es.First().Value));
                type(NcPGP.DBG_EXP, es => CDebug(CSeq(_ValueOf(es))));
                type(NcPGP.DBG_1EXP, es => CDebug(_ValueOf(es.First())));
                type(NcPGP.WORD_EXP, es =>
                {
                    var word = es.First().Value;
                    if (SplitWord)
                    {
                        return CSeq(word.Split()
                            .Where(s => s != string.Empty)
                            .Select(w => PEq(w, CaseSensitive))
                            .ToArray());
                    }
                    else
                    {
                        return PEq(word, CaseSensitive);
                    }
                });
                type(NcPGP.REGEX_EXP, es => PRegex(es.First().Value));
                type(NcPGP.VAR_EXP, es => Get(es.First().Value));
            });
        }

        private IParser[] _ValueOf(IEnumerable<Node> es)
        {
            return es.Select(_ValueOf).ToArray();
        }

        private IParser _ApplyOp(Node op, params IParser[] ps)
        {
            return Node.Match<IParser>(op, (type) =>
            {
                type(NcPGP.PLUS_CMB, _ => CPlus(ps));
                type(NcPGP.STAR_CMB, _ => CStar(ps));
                type(NcPGP.OR_CMB, _ => COr(ps));
                type(NcPGP.SEQ_CMB, _ => CSeq(ps));
                type(NcPGP.JOIN_CMB, _ => CJoin(ps.First(), ps.Skip(1).ToArray()));
                type(NcPGP.JOIN_PLUS_CMB, _ => CJoinPlus(ps.First(), ps.Skip(1).ToArray()));
                type(NcPGP.NOT_CMB, _ => CNot(ps));
                type(NcPGP.MAYBE_CMB, _ => CMaybe(ps));
            });
        }

        public static NcParser Load(string src, Action<NcParser> init = null)
        {
            var pgp = new NcPGP();
            var parseResult = pgp.ScanAndParse(src);
            if (!parseResult.IsSuccess())
            {
                throw new ParseException($"parsing grammer failed: (message: {parseResult.Message}, rest: {parseResult.FailRest.ToString()})");
            }
            return new NcParser(parseResult.Nodes[0], init);
        }

        public static NcParser LoadFromAssembly(Assembly assembly, string path, Action<NcParser> init = null)
        {
            using (var stream = assembly.GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                var src = reader.ReadToEnd();
                return Load(src, init);
            }
        }
    }
}
