using System.Collections.Generic;

namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public abstract class SyntaxBase : ISyntax
    {
        protected SyntaxBase(ItemSyntax item, IEnumerable<SectionSyntax> sections)
        {
        }

        public ISyntax ChildItem { get; private set; }
        public IEnumerable<SectionSyntax> Sections { get; private set; }
    }
}
