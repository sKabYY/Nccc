using Nccc.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Scanner
{
    public class CharScanner
    {
        public TokenStream Scan(string str)
        {
            return new TokenStream(this, str);
        }

        public Token Scan1(string str, TextPosition start)
        {
            if (str.Length == start.Offset)
            {
                return Token.Eof(start);
            }
            return Token.Make(str[start.Offset], start);
        }
    }
}
