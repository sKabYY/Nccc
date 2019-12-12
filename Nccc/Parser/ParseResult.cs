using Nccc.Common;
using Nccc.Scanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nccc.Parser
{
    public class ParseResult
    {
        public IList<Node> Nodes { get; set; }
        public bool Success { get; set; }
        public TokenStream Rest { get; set; }
        public string Message { get; set; }
        public TokenStream FailRest { get; set; }
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }
        public string ParserName { get; set; }

        public bool IsSuccess()
        {
            return Nodes != null && Success;
        }

        public ParseResult Deeper(ParseResult r)
        {
            if (r == null || r.FailRest.Position().Offset <= FailRest.Position().Offset)
            {
                return this;
            }
            else
            {
                return r;
            }
        }

        public ParseResult FailResult()
        {
            return new ParseResult
            {
                Success = false,
                Nodes = Nodes,
                Rest = Rest,
                Message = Message,
                FailRest = FailRest,
                Start = Start,
                End = End
            };
        }

        public SExp ToSExp()
        {
            var list = SExp.List(SExp.List("success?", IsSuccess()));
            if (IsSuccess())
            {
                list.Push(
                    SExp.List("nodes", SExp.List(Nodes?.Select(n => n.ToSExp()).ToArray())),
                    SExp.List("rest", Rest),
                    SExp.List("message", Message),
                    SExp.List("fail_rest", FailRest));
            }
            else
            {
                list.Push(
                    SExp.List("message", Message),
                    SExp.List("fail_rest", FailRest),
                    SExp.List("rest", Rest),
                    SExp.List("nodes", SExp.List(Nodes?.Select(n => n.ToSExp()).ToArray())));
            }
            if (!string.IsNullOrEmpty(ParserName))
            {
                list.PushFront(SExp.List(SExp.List("parser", ParserName)));
            }
            return list;
        }

        public override string ToString()
        {
            return ToSExp().ToPrettyString();
        }
    }
}
