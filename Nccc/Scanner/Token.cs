using Nccc.Common;
using Nccc.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Scanner
{
    public class Token
    {
        public char Value
        {
            get
            {
                if (_value.HasValue) return _value.Value;
                throw new ReachEofException();
            }
        }
        public TextPosition Start { get; }
        public TextPosition End { get; }

        private readonly char? _value;

        private Token(char? value, TextPosition start)
        {
            _value = value;
            Start = start;
            if (value.HasValue)
            {
                End = start.Shift(value.Value);
            } else
            {
                End = start;
            }
        }

        public static Token Make(char value, TextPosition start)
        {
            return new Token(value, start);
        }

        public static Token Eof(TextPosition start)
        {
            return new Token(null, start);
        }

        public bool IsEof()
        {
            return !_value.HasValue;
        }

        public override string ToString()
        {
            if (IsEof())
            {
                return $"<EOF at=({Start.Linenum}, {Start.Colnum})>";
            } else
            {
                return $"<Token char='{Value}', from=({Start.Linenum}, {Start.Colnum}), to=({End.Linenum}, {End.Colnum})>";
            }
        }
    }
}
