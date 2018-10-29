using System;

namespace Nccc
{
    public class TextPosition
    {
        public int Offset { get; }  // starts from 0

        public int Linenum { get; }  // starts from 1
        public int Colnum { get; }  // starts from 1

        public TextPosition(int offset, int linenum, int colnum)
        {
            Offset = offset;
            Linenum = linenum;
            Colnum = colnum;
        }

        public TextPosition Shift(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return this;
            var lines = prefix.Split('\n');  // TODO: .net 4.6  string.Split(string)
            if (lines.Length == 1)
            {
                return new TextPosition(Offset + prefix.Length, Linenum, Colnum + lines[0].Length);
            }
            else
            {
                return new TextPosition(Offset + prefix.Length, Linenum + lines.Length - 1, lines[0].Length + 1);
            }
        }

        public TextPosition Shift(char c)
        {
            if (c == '\n')
            {
                return new TextPosition(Offset + 1, Linenum + 1, 1);
            }
            else
            {
                return new TextPosition(Offset + 1, Linenum, Colnum + 1);
            }
        }

        public TextPosition ShiftToEnd(string str)
        {
            return Shift(str.Substring(Offset));
        }

        public static TextPosition StartPos { get; } = new TextPosition(0, 1, 1);
    }
}
