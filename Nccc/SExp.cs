using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nccc
{
    public abstract class SExp
    {
        public static ListSExp List(params object[] os)
        {
            if (os == null) return null;
            return new ListSExp(os);
        }

        public static ValueSExp Value(object obj)
        {
            return new ValueSExp(obj);
        }

        protected static SExp EnsureSExp(object obj)
        {
            return obj as SExp ?? new ValueSExp(obj);
        }

        public abstract string ToPrettyString(bool firstOfList = false, int indent = 0);
    }

    public class ListSExp: SExp
    {
        private IList<SExp> _sexps;

        public ListSExp(params object[] os)
        {
            _sexps = os.Select(EnsureSExp).ToList();
        }

        public void Push(params object[] os)
        {
            foreach (var o in os)
            {
                _sexps.Add(EnsureSExp(o));
            }
        }

        public void PushFront(object o)
        {
            _sexps.Insert(0, EnsureSExp(o));
        }

        public void Append(ListSExp e)
        {
            foreach (var o in e._sexps)
            {
                _sexps.Add(o);
            }
        }

        public override string ToPrettyString(bool sameLine = false, int indent = 0)
        {
            var sb = new StringBuilder();
            if (!sameLine)
            {
                sb.Append(new string(' ', indent));
            }
            sb.Append('(');
            if (_sexps.Count > 0)
            {
                ++indent;
                var first = _sexps.First().ToPrettyString(true, indent);
                sb.Append(first);
                if (_sexps.All(e => e is ValueSExp))
                {
                    var maxLineLength = 80;
                    var curLength = first.Length;
                    foreach (var sexp in _sexps.Skip(1))
                    {
                        if (curLength < maxLineLength)
                        {
                            var str = sexp.ToPrettyString(true, indent);
                            sb.Append(' ');
                            sb.Append(str);
                            curLength += str.Length + 1;
                        }
                        else
                        {
                            var str = sexp.ToPrettyString(false, indent);
                            sb.Append('\n');
                            sb.Append(str);
                            curLength = str.Length;
                        }
                    }
                }
                else
                {
                    foreach (var sexp in _sexps.Skip(1))
                    {
                        sb.Append('\n');
                        sb.Append(sexp.ToPrettyString(false, indent));
                    }
                }
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

    public class ValueSExp: SExp
    {
        private readonly object _value;
        public ValueSExp(object value)
        {
            _value = value;
        }

        public override string ToPrettyString(bool firstOfList, int indent)
        {
            var s = _value == null ? "<null>" : _value.ToString();
            if (_value is string && s.Any(Char.IsWhiteSpace))
            {
                s = $"\"{s.Replace("\"", "\\\"")}\"";
            }
            if (firstOfList || indent == 0) return s;
            return new string(' ', indent) + s;
        }
    }
}
