using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Exceptions
{
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }
}
