using Nccc.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Nccc.Parser
{
    public class NcGrammerParser : CombinedParser
    {
        public const string CASE_SENSITIVE = "case-sensitive";
        public const string SPLIT_WORD = "split-word";
        public const string LEX_IGNORE = "lex-ignore";
        public const string LEX_MODE = "lex-mode";
        public const string INCLUDE_BUILTIN = "include-builtin";

        public const string OPTION_ON = "on";
        public const string OPTION_OFF = "off";

        public const string SET_MESSAGE_LOCALE_START = "set-message-locale-start";
        public const string SET_MESSAGE_LOCALE_END = "set-message-locale-end";

        public const string NORMAL_CHAR = "normal-char";
        public const string SPECIAL_CHAR = "special-char";
        public const string SPECIAL_CHAR_EOF = "eof";
        public const string SPECIAL_CHAR_NEWLINE = "newline";
        public const string SPECIAL_CHAR_SPACE = "space";

        public const string PLUS_CMB = "@+";
        public const string STAR_CMB = "@*";
        public const string JOIN_CMB = "@,*";
        public const string JOIN_PLUS_CMB = "@,+";
        public const string OR_CMB = "@or";
        public const string SEQ_CMB = "@..";
        public const string NOT_CMB = "@!";
        public const string MAYBE_CMB = "@?";

        public const string EXP = "exp";
        public const string NAMED_EXP = "named-exp";
        public const string GLOB_EXP = "glob-exp";
        public const string OP_EXP = "op-exp";
        public const string SEQ_EXP = "seq-exp";
        public const string ERR_EXP = "err-exp";
        public const string ANY_EXP = "any-exp";
        public const string DBG_EXP = "dbg-exp";
        public const string DBG_1EXP = "dbg-1exp";
        public const string CHAR_EXP = "char-exp";
        public const string GLOB_CHAR_EXP = "glob-char-exp";
        public const string WORD_EXP = "word-exp";
        public const string GLOB_WORD_EXP = "glob-word-exp";
        public const string VAR_EXP = "var-exp";
        public const string PRIMARY_EXP = "primary-exp";
        public const string ARRAY_EXP = "array-exp";

        public const string OPTION_STM = "option-stm";
        public const string DEF_STM = "def-stm";

        public const string OPTION_SECTION = "option-section";
        public const string DEF_SECTION = "def-section";

        public const string DEF_ROOT = "def-root";
        public const string PROGRAM = "program";

        public NcGrammerParser()
        {
            var space = PSpace();
            var comment = COr(
                CSeq(PChar(';'), CStar(CNot(PNewline()), PAny())),
                CWrapperAnyBy(PCharArray("#|"), PCharArray("|#")));
            var spacing = CGlob(CStar(COr(space, comment)));
            SpacingParser = spacing;

            var iden_alpha = COr(PChar('_'), PChar('-'), PAlpha());
            var variable = CIfFail("invalid var", CSeq(iden_alpha, CStar(COr(iden_alpha, PDigit())), spacing));
            var charIdentifier = COr(
                CIs(SPECIAL_CHAR, COr(
                    PEqAndRetain(SPECIAL_CHAR_EOF),
                    PEqAndRetain(SPECIAL_CHAR_NEWLINE),
                    PEqAndRetain(SPECIAL_CHAR_SPACE))),
                CIs(NORMAL_CHAR, CIfFail("expect a char", CNot(space)), PAny(), spacing));
            var hchr = CSeq(CGlob(PCharArray("#\\")), charIdentifier);
            var chr = CSeq(CGlob(PChar('\\')), charIdentifier);
            var str = CSeq(CWrapperAnyBy(PChar('\''), PChar('\'')), spacing);
            var hstr = CSeq(CWrapperAnyBy(PCharArray("#'"), PChar('\'')), spacing);
            var integer = CSeq(CPlus(PDigit()), spacing);

            var on_or_off = COr(CIs(OPTION_ON, PEq(OPTION_ON)), CIs(OPTION_OFF, PEq(OPTION_OFF)));

            var case_sensitive_stm = CSeq(CIs(CASE_SENSITIVE, PEq("@case-sensitive")), on_or_off);
            var split_word_stm = CSeq(CIs(SPLIT_WORD, PEq("@split-word")), on_or_off);
            var lex_ignore_stm = CSeq(CIs(LEX_IGNORE, PEq("@lex-ignore")), CIs("spacing-parser", variable));
            var lex_mode_stm = CSeq(CIs(LEX_MODE, PEq("@lex-mode")));
            var include_builtin_stm = CSeq(CIs(INCLUDE_BUILTIN, PEq("@include-builtin")));
            var set_message_locale_start = CSeq(CIs(SET_MESSAGE_LOCALE_START, PEq("@set-message-locale-start")), CIs("locale-start", str));
            var set_message_locale_end = CSeq(CIs(SET_MESSAGE_LOCALE_END, PEq("@set-message-locale-end")), CIs("locale-end", str));
            var option_stm = CIs(OPTION_STM, COr(
                case_sensitive_stm,
                split_word_stm,
                lex_ignore_stm,
                lex_mode_stm,
                include_builtin_stm,
                set_message_locale_start,
                set_message_locale_end));

            var lparen = PEq("(");
            var rparen = PEq(")");
            var eq = PEq("=");
            var cmb_op = CIfFail("unknown op", CIs("op", COr(
                PEqAndRetain(PLUS_CMB),
                PEqAndRetain(STAR_CMB),
                PEqAndRetain(JOIN_CMB),
                PEqAndRetain(JOIN_PLUS_CMB),
                PEqAndRetain(OR_CMB),
                PEqAndRetain(SEQ_CMB),
                PEqAndRetain(NOT_CMB),
                PEqAndRetain(MAYBE_CMB))));

            var named_exp = CSeq(CIs("name", variable), PEq(":"), Get(EXP));
            var glob_exp = CSeq(PEq("~"), Get(EXP));
            var op_exp = CSeq(lparen, cmb_op, CStar(Get(EXP)), rparen);
            var err_exp = CSeq(lparen, PEq("@err"), CIs("message", str), CPlus(Get(EXP)), rparen);
            var seq_exp = CSeq(lparen, CPlus(Get(EXP)), rparen);
            var any_exp = PEq("<*>");
            var array_exp = CSeq(PEq("["), CIs("size", integer), PEq("]"), Get(EXP));
            var dbg_exp = CSeq(PEq("?["), CPlus(Get(EXP)), PEq("]"));
            var dbg_1exp = CSeq(PEq("??"), Get(EXP));
            var exp = DefParser(EXP, COr(
                CIs(NAMED_EXP, named_exp),
                CIs(GLOB_EXP, glob_exp),
                CIs(OP_EXP, op_exp),
                CIs(ERR_EXP, err_exp),
                CIs(SEQ_EXP, seq_exp),
                CIs(ANY_EXP, any_exp),
                CIs(ARRAY_EXP, array_exp),
                CIs(DBG_EXP, dbg_exp),
                CIs(DBG_1EXP, dbg_1exp),
                CIs(CHAR_EXP, hchr),
                CIs(GLOB_CHAR_EXP, chr),
                CIs(WORD_EXP, hstr),
                CIs(GLOB_WORD_EXP, str),
                CIs(VAR_EXP, CSeq(variable, CNot(PEq(":"))))));

            var def_stm = CIs(DEF_STM, CSeq(CIs("lhs", variable), eq, CPlus(exp, CNot(eq))));
            var root_stm = CIs(DEF_ROOT, CSeq(PEq("::"), variable));

            SetRootParser(DefParser(PROGRAM, CIs(PROGRAM, CSeq(
                root_stm,
                CIs(OPTION_SECTION, CStar(option_stm)),
                CIs(DEF_SECTION, CStar(def_stm))))));
        }

        private IParser CWrapperAnyBy(IParser start, IParser end)
        {
            return CSeq(CGlob(start), CStar(CNot(end), PAny()), CGlob(end));
        }

        public static string GetNcGrammerSource()
        {
            return Assembly.GetExecutingAssembly().ReadString("Nccc.Bootstrapping.nccc.grammer");
        }

    }
}
