using Nccc.Common;
using Nccc.Exceptions;
using System.Collections.Generic;

namespace Nccc.Scanner
{
    public class TokenStream
    {
        private readonly CharScanner _scanner;
        private readonly Token _cur;
        public string Str { get; }

        public TokenStream(CharScanner charScanner, string str, TextPosition pos)
        {
            _scanner = charScanner;
            Str = str;
            _cur = charScanner.Scan1(Str, pos);
        }

        public TokenStream(CharScanner charScanner, string str) : this(charScanner, str, TextPosition.StartPos)
        {
        }


        public Token Car()
        {
            return _cur;
        }

        public TokenStream Cdr()
        {
            return new TokenStream(_scanner, Str, _cur.End);
        }

        public bool IsEof()
        {
            return _cur.IsEof();
        }

        public TextPosition Position()
        {
            return _cur.Start;
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
            hashCode = hashCode * -1521134295 + EqualityComparer<CharScanner>.Default.GetHashCode(_scanner);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Str);
            hashCode = hashCode * -1521134295 + EqualityComparer<Token>.Default.GetHashCode(_cur);
            return hashCode;
        }
    }
}
