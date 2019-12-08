using Nccc.Common;
using Nccc.Scanner;
using System.Collections.Generic;
using System.Text;

namespace Nccc.Parser
{
    public interface IParseStack
    {
        bool IsEmpty();
        bool Has(IParser parser, TokenStream toks);
        IParseStack Extend(IParser parser, TokenStream toks);
        ListSExp ToSExp();
    }
}
