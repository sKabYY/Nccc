﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nccc
{
    public class Scanner
    {
        public string[] Delims { get; set; }
        public string[] LineComment { get; set; }
        public string CommentStart { get; set; }
        public string CommentEnd { get; set; }
        public string[] Operators { get; set; }
        public string[] QuotationMarks { get; set; }
        public string[] RegexMarks { get; set; }
        public string[] LispChar { get; set; }
        public string NumberRegex { get; set; }
        public string[] SignificantWhitespaces { get; set; }

        public bool CharMode { get; set; } = false;

        public const string NumberPattern = "([+-]?\\d+(\\.\\d+)?([Ee]-?\\d+)?)";

        public Locale _ { get; }
        public void SetLanguage(string lang)
        {
            _.Language = lang;
        }

        public Scanner(Locale locale = null)
        {
            // TODO: default all empty
            // s-expression settings by default
            // please override for other languages
            Delims = new string[] { "(", ")", "[", "]", "{", "}", "\'", "`", "," };
            LineComment = new string[] { ";" };
            CommentStart = null;
            CommentEnd = null;
            Operators = new string[] { };
            QuotationMarks = new string[] { "\"" };
            RegexMarks = new string[] { };
            LispChar = new string[] { };
            NumberRegex = NumberPattern;
            SignificantWhitespaces = new string[] { };
            _ = locale ?? new Locale();
        }

        public TokenStream Scan(string str)
        {
            if (str == null) throw new ScanException("input is null", TextPosition.StartPos, TextPosition.StartPos);
            return new TokenStream(this, str);
        }

        private class _MatchResult
        {
            public _MatchResult(string text, TextPosition end)
            {
                Text = text;
                End = end;
            }
            public string Text { get; set; }
            public TextPosition End { get; set; }
        }

        private static bool _StartsWithFromOffset(string str, int offset, string prefix)
        {
            if (str.Length - offset < prefix.Length) return false;
            for (var i = 0; i < prefix.Length; ++i)
            {
                if (str[offset + i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static _MatchResult _StartsWith(string str, TextPosition start, string prefix)
        {
            if (prefix == null) return null;
            if (_StartsWithFromOffset(str, start.Offset, prefix))
            {
                return new _MatchResult(prefix, start.Shift(prefix));
            }
            else
            {
                return null;
            }
        }

        private static _MatchResult _StartsWithOneOf(string str, TextPosition start, string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                var mr = _StartsWith(str, start, prefix);
                if (mr != null) return mr;
            }
            return null;
        }

        private static TextPosition _FindNext(string str, TextPosition start, string prefix)
        {
            for (var i = start.Offset; i < str.Length; ++i)
            {
                if (_StartsWithFromOffset(str, i, prefix))
                {
                    return start.Shift(str.Substring(start.Offset, i - start.Offset));
                }
            }
            return null;
        }

        private static Match _MatchFrom(string str, TextPosition start, string pattern)
        {
            return new Regex($"^{pattern}").Match(str, start.Offset, str.Length - start.Offset);
        }

        private Token _DefaultScan1(string str, TextPosition start)
        {
            _MatchResult mr;
            do
            {
                if (str.Length == start.Offset)
                {
                    return Token.MakeEof(start);
                }
                mr = _StartsWithOneOf(str, start, SignificantWhitespaces);
                if (mr != null)
                {
                    return Token.MakeNewline(mr.Text, start, mr.End);
                }
                if (char.IsWhiteSpace(str[start.Offset]))
                {
                    // 这里本来是个尾递归，但是C#没有明确是否优化，只好改成丑陋的迭代
                    start = start.Shift(str[start.Offset]);
                }
                else
                {
                    break;
                }
            } while (true);
            // block comment
            mr = _StartsWith(str, start, CommentStart);
            if (mr != null)
            {
                var end = _FindNext(str, mr.End, CommentEnd);
                if (end == null) throw new ScanException(_.L("block comment match error"), start, start.ShiftToEnd(str));
                end = end.Shift(CommentEnd);
                return Token.MakeComment(str.Substring(start.Offset, end.Offset - start.Offset), start, end);
            }
            // line comment
            mr = _StartsWithOneOf(str, start, LineComment);
            if (mr != null)
            {
                var end = _FindNext(str, mr.End, "\n");
                if (end == null) end = start.ShiftToEnd(str);
                return Token.MakeComment(str.Substring(start.Offset, end.Offset - start.Offset), start, end);
            }
            // delim
            mr = _StartsWithOneOf(str, start, Delims);
            if (mr != null)
            {
                return Token.MakeToken(mr.Text, start, mr.End);
            }
            // operator
            mr = _StartsWithOneOf(str, start, Operators);
            if (mr != null)
            {
                return Token.MakeToken(mr.Text, start, mr.End);
            }
            // string
            mr = _StartsWithOneOf(str, start, QuotationMarks);
            if (mr != null)
            {
                var mark = mr.Text;
                var matchData = _MatchFrom(str, start, $"{mark}(\\\\{mark}|[^{mark}])*{mark}");
                if (!matchData.Success)
                {
                    throw new ScanException(_.L("string match error"), start, start.ShiftToEnd(str));
                }
                var matchText = matchData.Value;
                var text = matchText.Substring(mark.Length, matchText.Length - 2 * mark.Length)
                    .Replace($"\\{mark}", mark)
                    .Replace(@"\n", "\n")
                    .Replace(@"\r", "\r")
                    .Replace(@"\t", "\t")
                    .Replace(@"\\", @"\");
                return Token.MakeStr(text, start, start.Shift(matchText));
            }
            // regex
            mr = _StartsWithOneOf(str, start, RegexMarks);
            if (mr != null)
            {
                var mark = mr.Text;
                var matchData = _MatchFrom(str, start, $"{mark}(\\\\{mark}|[^{mark}])*{mark}");
                if (!matchData.Success)
                {
                    throw new ScanException(_.L("regex match error"), start, start.ShiftToEnd(str));
                }
                var matchText = matchData.Value;
                var text = matchText.Substring(mark.Length, matchText.Length - 2 * mark.Length).Replace($"\\{mark}", mark);
                return Token.MakeRegex(text, start, start.Shift(matchText));
            }
            // TODO: scheme/elisp char
            // TODO: more literal type
            if (NumberRegex != null)
            {
                var matchData = _MatchFrom(str, start, NumberRegex);
                if (matchData.Success)
                {
                    var matchText = matchData.Value;
                    return Token.MakeNumber(matchText, start, start.Shift(matchText));
                }
            }
            // identifier or literal type
            var sb = new StringBuilder();
            var pos = start;
            while (pos.Offset < str.Length)
            {
                var c = str[pos.Offset];
                if (!char.IsWhiteSpace(c) &&
                    _StartsWithOneOf(str, pos, LineComment) == null &&
                    _StartsWith(str, pos, CommentStart) == null &&
                    _StartsWith(str, pos, CommentEnd) == null &&
                    _StartsWithOneOf(str, pos, QuotationMarks) == null &&
                    _StartsWithOneOf(str, pos, RegexMarks) == null &&
                    _StartsWithOneOf(str, pos, Delims) == null &&
                    _StartsWithOneOf(str, pos, Operators) == null)
                {
                    sb.Append(c);
                    pos = pos.Shift(c);
                }
                else
                {
                    break;
                }
            }
            return Token.MakeToken(sb.ToString(), start, pos);
        }

        private Token _CharModeScan1(string str, TextPosition start)
        {
            if (str.Length == start.Offset)
            {
                return Token.MakeEof(start);
            }
            var c = str[start.Offset].ToString();
            return Token.MakeToken(c, start, start.Shift(c));
        }

        public Token Scan1(string str, TextPosition start)
        {
            // ugly implementation
            // TODO: 抽象IScanner接口
            if (CharMode)
            {
                return _CharModeScan1(str, start);
            } else
            {
                return _DefaultScan1(str, start);
            }
        }
    }

    public enum TokenType
    {
        Eof,
        Comment,
        Newline,
        Regex,
        Str,
        Number,
        Token,
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Text { get; set; }
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }

        public override string ToString()
        {
            return $"<Token type={Type}, text=\"{Text}\", from=({Start.Linenum}, {Start.Colnum}), to=({End.Linenum}, {End.Colnum})>";
        }

        internal static Token MakeEof(TextPosition pos)
        {
            return new Token
            {
                Type = TokenType.Eof,
                Start = pos,
                End = pos
            };
        }

        internal static Token MakeComment(string text, TextPosition start, TextPosition end)
        {
            return new Token
            {
                Type = TokenType.Comment,
                Text = text,
                Start = start,
                End = end
            };
        }

        internal static Token MakeNewline(string text, TextPosition start, TextPosition end)
        {
            return new Token
            {
                Type = TokenType.Newline,
                Text = text,
                Start = start,
                End = end
            };
        }

        internal static Token MakeRegex(string text, TextPosition start, TextPosition end)
        {
            return new Token
            {
                Type = TokenType.Regex,
                Text = text,
                Start = start,
                End = end
            };
        }

        internal static Token MakeStr(string text, TextPosition start, TextPosition end)
        {
            return new Token
            {
                Type = TokenType.Str,
                Text = text,
                Start = start,
                End = end
            };
        }

        public static Token MakeNumber(string text, TextPosition start, TextPosition end)
        {
            return new Token
            {
                Type = TokenType.Number,
                Text = text,
                Start = start,
                End = end
            };
        }

        internal static Token MakeToken(string text, TextPosition start, TextPosition end)
        {
            return new Token
            {
                Type = TokenType.Token,
                Text = text,
                Start = start,
                End = end
            };
        }

        public bool IsEof()
        {
            return Type == TokenType.Eof;
        }

        internal bool IsComment()
        {
            return Type == TokenType.Comment;
        }

        internal bool IsToken()
        {
            return Type == TokenType.Token;
        }

        public bool IsNewline()
        {
            return Type == TokenType.Newline;
        }
    }

    abstract public class BaseStream<T>
    {
        public IList<T> ToList()
        {
            var list = new List<T>();
            var s = this;
            while (!s.IsEof())
            {
                list.Add(s.Car());
                s = s.Cdr();
            }
            return list;
        }

        abstract public T Car();
        abstract public BaseStream<T> Cdr();
        abstract public bool IsEof();
        abstract public TextPosition Position();

        public override string ToString()
        {
            return $"<Stream: car={Car().ToString()}>";
        }
    }

    public class FilteredStream<T> : BaseStream<T>
    {
        private readonly BaseStream<T> _stream;
        private readonly Func<T, bool> _pred;

        public FilteredStream(BaseStream<T> stream, Func<T, bool> pred)
        {
            while (!(stream.IsEof() || pred(stream.Car())))
            {
                stream = stream.Cdr();
            }
            _stream = stream;
            _pred = pred;
        }

        public override T Car()
        {
            return _stream.Car();
        }

        public override BaseStream<T> Cdr()
        {
            return new FilteredStream<T>(_stream.Cdr(), _pred);
        }

        public override bool IsEof()
        {
            return _stream.IsEof();
        }

        public override TextPosition Position()
        {
            return _stream.Position();
        }

        public override string ToString()
        {
            return _stream.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FilteredStream<T> s)) return false;
            return _stream.Equals(s._stream);
        }

        public override int GetHashCode()
        {
            var hashCode = -631423433;
            hashCode = hashCode * -1521134295 + EqualityComparer<BaseStream<T>>.Default.GetHashCode(_stream);
            hashCode = hashCode * -1521134295 + EqualityComparer<Func<T, bool>>.Default.GetHashCode(_pred);
            return hashCode;
        }
    }

    public class TokenStream: BaseStream<Token>
    {
        private readonly Scanner _scanner;
        private readonly Token _cur;

        public string Str { get; }

        public TokenStream(Scanner scanner, string str, TextPosition pos)
        {
            _scanner = scanner;
            Str = str;
            _cur = scanner.Scan1(Str, pos);
        }

        public TokenStream(Scanner scanner, string str) : this(scanner, str, TextPosition.StartPos)
        {
        }

        public override Token Car()
        {
            return _cur;
        }

        public override BaseStream<Token> Cdr()
        {
            return new TokenStream(_scanner, Str, _cur.End);
        }

        public override bool IsEof()
        {
            return _cur.IsEof();
        }

        public override TextPosition Position()
        {
            return _cur.Start;
        }

        public FilteredStream<Token> FilterComment()
        {
            return new FilteredStream<Token>(this, tok => !tok.IsComment());
        }

        public override string ToString()
        {
            return $"<Stream: car={Car().ToString()}>";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TokenStream s)) return false;
            return _scanner == s._scanner && Str == s.Str && Position() == s.Position();
        }

        public override int GetHashCode()
        {
            var hashCode = 1851490821;
            hashCode = hashCode * -1521134295 + EqualityComparer<Scanner>.Default.GetHashCode(_scanner);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Str);
            hashCode = hashCode * -1521134295 + EqualityComparer<Token>.Default.GetHashCode(_cur);
            return hashCode;
        }
    }

    public class ScanException: Exception
    {
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }

        public ScanException(string message, TextPosition start, TextPosition end)
            : base(message)
        {
            Start = start;
            End = end;
        }
    }
}
