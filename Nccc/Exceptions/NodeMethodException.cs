using Nccc.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Exceptions
{
    public class NodeMethodException : Exception
    {
        public Node Node { get; set; }
        public NodeMethodException(Node node, string message) : base(message)
        {
            Node = node;
        }
    }
}
