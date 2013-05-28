using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public abstract class SyntaxBase : ISyntax
    {
        protected SyntaxBase(IItemSyntax childItem, IEnumerable<ISectionSyntax> sections)
        {
            Sections = sections;
            ChildItem = childItem;
        }

        public IItemSyntax ChildItem { get; private set; }
        public IEnumerable<ISectionSyntax> Sections { get; private set; }
    }
}
