using System.Collections.Generic;
using System.Linq;

namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public class RecursiveItemSyntax : ISyntax
    {
        public ISyntax ChildItem
        {
            get { return this; }
        }

        public IEnumerable<SectionSyntax> Sections
        {
            get { return Enumerable.Empty<SectionSyntax>(); }
        }
    }
}