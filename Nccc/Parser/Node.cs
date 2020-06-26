using Nccc.Common;
using Nccc.Exceptions;
using Nccc.Scanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nccc.Parser
{
    public class Node
    {
        public string Type { get; set; }
        public char Value { get; set; }
        public IList<Node> Children { get; set; }
        public TextPosition Start { get; set; }
        public TextPosition End { get; set; }

        public bool IsLeaf()
        {
            return Children == null;
        }

        public bool IsStringNode()
        {
            return Children != null && Children.All(n => n.IsLeaf());
        }

        public bool TryGetStringValue(out string value)
        {
            if (IsStringNode())
            {
                value = NodesToString(Children);
                return true;
            }
            value = null;
            return false;
        }

        public string StringValue()
        {
            if (TryGetStringValue(out var value))
            {
                return value;
            }
            throw new NodeMethodException(this, $"can't get StringValue of node {ToSExp().ToPrettyString()} at row {Start.Linenum} colum {Start.Colnum}");
        }

        public static string ConcatValue(IList<Node> nodes)
        {
            var node = nodes.FirstOrDefault(n => !n.IsLeaf());
            if (node != null)
            {
                var pos = node.Start;
                throw new NodeMethodException(node, $"expect a leaf but got {node.ToSExp().ToPrettyString()} at row {pos.Linenum} column {pos.Colnum}");
            }
            return NodesToString(nodes);
        }

        private static string NodesToString(IList<Node> nodes)
        {
            return new string(nodes.Select(n => n.Value).ToArray());
        }

        public static Node MakeLeaf(Token tok)
        {
            return new Node
            {
                Value = tok.Value,
                Start = tok.Start,
                End = tok.End
            };
        }

        public static Node MakeNode(string type, IList<Node> children, TextPosition start, TextPosition end)
        {
            return new Node
            {
                Type = type,
                Children = children,
                Start = start,
                End = end
            };
        }

        public SExp ToSExp()
        {
            if (IsLeaf())
            {
                return SExp.Value(Value);
            } else if (TryGetStringValue(out var value))
            {
                var lst = SExp.List(GetNodeMeta());
                if (!string.IsNullOrEmpty(value))
                {
                    lst.Push(value);
                }
                return lst;
            } else
            {
                var lst = SExp.List(Children.Select(n => n.ToSExp()).ToArray());
                lst.PushFront(GetNodeMeta());
                return lst;
            }
        }

        public override string ToString()
        {
            return ToSExp().ToPrettyString();
        }

        private string GetNodeMeta()
        {
            return $"{Type}[({Start?.Linenum},{Start?.Colnum})-({End?.Linenum},{End?.Colnum})]";
        }

        public void Match(Action<Action<string, Action<IList<Node>>>> block)
        {
            var hit = false;
            block((type, proc) =>
            {
                if (Type == type)
                {
                    proc(Children);
                    hit = true;
                }
            });
            if (!hit) throw new NodeMethodException(this, $"match error: unknown type \"{Type}\" for node {ToSExp().ToPrettyString()}");
        }

        public static void Match(IList<Node> nodes, Action<Action<string, Action<IList<Node>>>> block)
        {
            foreach (var node in nodes)
            {
                node.Match(block);
            }
        }

        public T Match<T>(Action<Action<string, Func<IList<Node>, T>>> block)
        {
            var value = default(T);
            var hit = false;
            block((type, proc) =>
            {
                if (Type == type)
                {
                    value = proc(Children);
                    hit = true;
                }
            });
            if (!hit) throw new NodeMethodException(this, $"match error: unknown type \"{Type}\" for node {ToSExp().ToPrettyString()}");
            return value;
        }

        public T DigSomething<T>(Func<Node, T> foundOne, Func<string, Node, T> notFound, Func<string, Node, T> foundMore, params string[] path)
        {
            var node = this;
            for (var i = 0; i < path.Length; ++i)
            {
                if (node.IsLeaf())
                {
                    return notFound(path[i], node);
                }
                var found = node.Children.Where(n => n.Type == path[i]).ToArray();
                switch (found.Length)
                {
                    case 0:
                        return notFound(path[i], node);
                    case 1:
                        node = found.First();
                        break;
                    default:
                        return foundMore(path[i], node);
                }
            }
            return foundOne(node);
        }

        public Node DigNode(params string[] path)
        {
            return DigSomething(
                node => node,
                (type, node) => throw new NodeMethodException(this, $"type \"{type}\" not found in ${node.ToSExp().ToPrettyString()}"),
                (type, node) => throw new NodeMethodException(this, $"two or more \"{type}\" found in ${node.ToSExp().ToPrettyString()}"),
                path);
        }

        public Node DigNodeOrNull(params string[] path)
        {
            if (TryDigNode(out var node, path))
            {
                return node;
            }
            return null;
        }

        public bool TryDigNode(out Node node, params string[] path)
        {
            Node nd = null;
            var found = DigSomething(
                n => { nd = n; return true; },
                (type, n) => false,
                (type, n) => false,
                path);
            node = nd;
            return found;
        }

        public static Node DigNode(IList<Node> nodes, params string[] path)
        {
            return new Node { Children = nodes }.DigNode(path);
        }

        public static bool TryDigNode(IList<Node> nodes, out Node node, params string[] path)
        {
            return new Node { Children = nodes }.TryDigNode(out node, path);
        }

        public static Node DigNodeOrNull(IList<Node> nodes, params string[] path)
        {
            if (TryDigNode(nodes, out var node, path))
            {
                return node;
            }
            return null;
        }

        public string DigValue(params string[] path)
        {
            return DigNode(path).StringValue();
        }

        public string DigValueOrNull(params string[] path)
        {
            if (TryDigValue(out var value, path))
            {
                return value;
            }
            return null;
        }

        public bool TryDigValue(out string value, params string[] path)
        {
            if (TryDigNode(out Node node, path))
            {
                value = node.StringValue();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static string DigValue(IList<Node> nodes, params string[] path)
        {
            return DigNode(nodes, path).StringValue();
        }
    }
}
