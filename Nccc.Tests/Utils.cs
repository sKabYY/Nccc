using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Nccc.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests
{
    static class Utils
    {
        public static DiffPiece[] DiffAndShow(ParseResult before, ParseResult after)
        {
            return DiffAndShow(before.ToSExp().ToPrettyString(), after.ToSExp().ToPrettyString());
        }

        public static DiffPiece[] DiffAndShow(string before, string after)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(before, after);
            var oldColor = Console.ForegroundColor;
            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("+ ");
                        break;
                    case ChangeType.Deleted:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("- ");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("  ");
                        break;
                }
                Console.WriteLine(line.Text);
            }
            Console.ForegroundColor = oldColor;
            return diff.Lines.Where(line => line.Type != ChangeType.Unchanged).ToArray();
        }
    }
}
