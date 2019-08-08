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
        public const string SET_DELIMS = "set-delims";
        public const string SET_LINE_COMMENT = "set-line-comment";
        public const string SET_COMMENT_START = "set-comment-start";
        public const string SET_COMMENT_END = "set-comment-end";
        public const string SET_OPERATORS = "set-operators";
        public const string SET_QUOTATION_MARKS = "set-quotation-marks";
        public const string SET_REGEX_MARKS = "set-regex-marks";
        public const string SET_LISP_CHAR = "set-lisp-char";
        public const string SET_NUMBER_REGEX = "set-number-regex";
        public const string SET_SIGNIFICANT_WHITESPACES = "set-significant-whitespaces";
        public const string CASE_SENSITIVE = "case-sensitive";
        public const string SPLIT_WORD = "split-word";

        public const string LEFT_RECUR_DETECTION = "left-recur-detection";
        public const string USE_MEMORIZED_PARSER = "use-memorized-parser";
        public const string OPTION_ON = "on";
        public const string OPTION_OFF = "off";

        public const string SET_MESSAGE_LOCALE_START = "set-message-locale-start";
        public const string SET_MESSAGE_LOCALE_END = "set-message-locale-end";

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
        public const string ERR_EXP = "err-exp";
        public const string ANY_EXP = "any-exp";
        public const string TOKEN_TYPE_EXP = "token-type-exp";
        public const string DBG_EXP = "dbg-exp";
        public const string DBG_1EXP = "dbg-1exp";
        public const string WORD_EXP = "word-exp";
        public const string REGEX_EXP = "regex-exp";
        public const string VAR_EXP = "var-exp";

        public const string SCANNER_OPTION_STM = "scanner-option-stm";
        public const string PARSER_OPTION_STM = "parser-option-stm";
        public const string LOCALE_OPTION_STM = "locale-option-stm";
        public const string DEF_STM = "def-stm";

        public const string OPTION_SECTION = "option-section";
        public const string DEF_SECTION = "def-section";

        public const string DEF_ROOT = "def-root";
        public const string PROGRAM = "program";

        public static void InitScanner(Scanner scanner)
        {
            scanner.Delims = new string[] { "(", ")", "[", "]", "{", "}", "<", ">", "`", "::", "=", ":", "~", "??" };
            scanner.LineComment = new string[] { ";" };
            scanner.CommentStart = "#|";
            scanner.CommentEnd = "|#";
            scanner.Operators = new string[] { };
            scanner.QuotationMarks = new string[] { "'" };
            scanner.RegexMarks = new string[] { "/" };
            scanner.LispChar = new string[] { };
            scanner.NumberRegex = Scanner.NumberPattern;
            scanner.SignificantWhitespaces = new string[] { };
        }

        public NcPGP()
        {
            InitScanner(Scanner);

            var scanner_option = CIfFail("unknown scanner option", COr(
                CIs(SET_DELIMS, PEq("@set-delims")),
                CIs(SET_LINE_COMMENT, PEq("@set-line-comment")),
                CIs(SET_COMMENT_START, PEq("@set-comment-start")),
                CIs(SET_COMMENT_END, PEq("@set-comment-end")),
                CIs(SET_OPERATORS, PEq("@set-operators")),
                CIs(SET_QUOTATION_MARKS, PEq("@set-quotation-marks")),
                CIs(SET_REGEX_MARKS, PEq("@set-regex-marks")),
                CIs(SET_LISP_CHAR, PEq("@set-lisp-char")),
                CIs(SET_NUMBER_REGEX, PEq("@set-number-regex")),
                CIs(SET_SIGNIFICANT_WHITESPACES, PEq("@set-significant-whitespaces"))));

            var parser_option = CIfFail("unknown parser option", COr(
                CIs(CASE_SENSITIVE, PEq("@case-sensitive")),
                CIs(SPLIT_WORD, PEq("@split-word")),
                CIs(LEFT_RECUR_DETECTION, PEq("@left-recur-detection")),
                CIs(USE_MEMORIZED_PARSER, PEq("@use-memorized-parser"))));

            var locale_option = CIfFail("unknown locale option", COr(
                CIs(SET_MESSAGE_LOCALE_START, PEq("@set-message-locale-start")),
                CIs(SET_MESSAGE_LOCALE_END, PEq("@set-message-locale-end"))));

            var on_or_off = COr(CIs(OPTION_ON, PEq("on")), CIs(OPTION_OFF, PEq("off")));
            var scanner_option_stm = CIs(SCANNER_OPTION_STM, CSeq(scanner_option, CStar(PTokenType(TokenType.Str))));
            var parser_option_stm = CIs(PARSER_OPTION_STM, CSeq(parser_option, on_or_off));
            var locale_option_stm = CIs(LOCALE_OPTION_STM, CSeq(locale_option, CMaybe(PTokenType(TokenType.Str))));

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
            var err_exp = CSeq(lparen, PEq("@err"), PTokenType(TokenType.Str), CPlus(Get(EXP)), rparen);
            var seq_exp = CSeq(lparen, CPlus(Get(EXP)), rparen);
            var dbg_exp = CSeq(PEq("["), CPlus(Get(EXP)), PEq("]"));
            var any_exp = CSeq(PEq("<"), PEq("*"), PEq(">"));
            var token_type_exp = CSeq(PEq("<"), PRegex(variablePattern), PEq(">"));
            var dbg_1exp = CSeq(PEq("??"), Get(EXP));
            var exp = DefParser(EXP, COr(
                CIs(NAMED_EXP, named_exp),
                CIs(GLOB_EXP, glob_exp),
                CIs(OP_EXP, op_exp),
                CIs(ERR_EXP, err_exp),
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

            RootParser = DefParser(PROGRAM, CIs(PROGRAM, CSeq(
                root_stm,
                CIs(OPTION_SECTION, CStar(COr(scanner_option_stm, parser_option_stm, locale_option_stm))),
                CIs(DEF_SECTION, CStar(def_stm)))));
        }

        public static string ReadStringFromAssembly(Assembly assembly, string path)
        {
            using (var stream = assembly.GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetNcGrammerSource()
        {
            return ReadStringFromAssembly(Assembly.GetExecutingAssembly(),
                "Nccc.nccc.grammer");
        }
    }

    public class NcParser: Parsec
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

            public Scanner Scanner => _parser.Scanner;

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

        private NcParser(Node grammerAst, Action<Settings> init)
        {
            CaseSensitive = true;
            SplitWord = true;
            LeftRecurDetection = true;
            UseMemorizedParser = true;

            var nodes = grammerAst.Children;
            var rootStm = nodes.First();
            var optionStms = nodes[1].Children;
            var defStms = nodes[2].Children;
            var rootName = rootStm.LeafValue();

            Node.Match(optionStms, type =>
            {
                type(NcPGP.SCANNER_OPTION_STM, es =>
                {
                    var values = es.Skip(1).Select(e => e.Value).ToArray();
                    _SetScannerOption(es.First(), values);
                });
                type(NcPGP.PARSER_OPTION_STM, es =>
                {
                    var isOn = es[1].Match<bool>(t =>
                    {
                        t(NcPGP.OPTION_ON, _ => true);
                        t(NcPGP.OPTION_OFF, _ => false);
                    });
                    _SetParserOption(es.First(), isOn);
                });
                type(NcPGP.LOCALE_OPTION_STM, es =>
                {
                    var values = es.Skip(1).Select(e => e.Value).ToArray();
                    _SetLocaleOption(es.First(), values);
                });
            });

            init?.Invoke(new Settings(this));

            Node.Match(defStms, type =>
            {
                type(NcPGP.DEF_STM, es =>
                {
                    var name = es.First().Value;
                    var ps = _ValueOf(es.Skip(1));
                    DefParser(name, CSeq(ps));
                });
            });

            RootParser = Get(rootName);
        }

        private string _GetAtMostOneValue(Node name, string[] values)
        {
            switch (values.Count())
            {
                case 0: return null;
                case 1: return values.First();
                default: throw new ParseException($"expect at most one value for {name.ToSExp().ToPrettyString()}");
            }
        }

        private void _SetScannerOption(Node name, string[] values)
        {
            switch (name.Type)
            {
                case NcPGP.SET_DELIMS:
                    Scanner.Delims = values;
                    break;
                case NcPGP.SET_LINE_COMMENT:
                    Scanner.LineComment = values;
                    break;
                case NcPGP.SET_COMMENT_START:
                    Scanner.CommentStart = _GetAtMostOneValue(name, values);
                    break;
                case NcPGP.SET_COMMENT_END:
                    Scanner.CommentEnd = _GetAtMostOneValue(name, values);
                    break;
                case NcPGP.SET_OPERATORS:
                    Scanner.Operators = values;
                    break;
                case NcPGP.SET_QUOTATION_MARKS:
                    Scanner.QuotationMarks = values;
                    break;
                case NcPGP.SET_REGEX_MARKS:
                    Scanner.RegexMarks = values;
                    break;
                case NcPGP.SET_LISP_CHAR:
                    Scanner.LispChar = values;
                    break;
                case NcPGP.SET_NUMBER_REGEX:
                    Scanner.NumberRegex = _GetAtMostOneValue(name, values);
                    break;
                case NcPGP.SET_SIGNIFICANT_WHITESPACES:
                    Scanner.SignificantWhitespaces = values;
                    break;
            }
        }

        private void _SetParserOption(Node name, bool isOn)
        {
            switch (name.Type)
            {
                case NcPGP.CASE_SENSITIVE:
                    CaseSensitive = isOn;
                    break;
                case NcPGP.SPLIT_WORD:
                    SplitWord = isOn;
                    break;
                case NcPGP.LEFT_RECUR_DETECTION:
                    LeftRecurDetection = isOn;
                    break;
                case NcPGP.USE_MEMORIZED_PARSER:
                    UseMemorizedParser = isOn;
                    break;
            }
        }

        private void _SetLocaleOption(Node name, string[] values)
        {
            switch (name.Type)
            {
                case NcPGP.SET_MESSAGE_LOCALE_START:
                    MessageLocaleStart = _GetAtMostOneValue(name, values);
                    break;
                case NcPGP.SET_MESSAGE_LOCALE_END:
                    MessageLocaleEnd = _GetAtMostOneValue(name, values);
                    break;
            }
        }

        private IParser _ValueOf(Node exp)
        {
            return exp.Match<IParser>(type =>
            {
                type(NcPGP.NAMED_EXP, es => CIs(es.First().Value, _ValueOf(es[1])));
                type(NcPGP.GLOB_EXP, es => CGlob(_ValueOf(es.First())));
                type(NcPGP.OP_EXP, es => _ApplyOp(es.First(), _ValueOf(es.Skip(1))));
                type(NcPGP.ERR_EXP, es => CIfFail(es.First().Value, CSeq(_ValueOf(es.Skip(1)))));
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
            return op.Match<IParser>(type =>
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

        public static NcParser Load(string src, Action<Settings> init = null)
        {
            var pgp = new NcPGP();
            var parseResult = pgp.ScanAndParse(src);
            if (!parseResult.IsSuccess())
            {
                throw new ParseException($"parsing grammer failed: (message: {parseResult.Message}, rest: {parseResult.FailRest.ToString()})");
            }
            return new NcParser(parseResult.Nodes.First(), init);
        }

        public static NcParser Load(Node grammerAst, Action<Settings> init = null)
        {
            return new NcParser(grammerAst, init);
        }

        public static NcParser LoadFromAssembly(Assembly assembly, string path, Action<Settings> init = null)
        {
            var src = NcPGP.ReadStringFromAssembly(assembly, path);
            return Load(src, init);
        }
    }
}
