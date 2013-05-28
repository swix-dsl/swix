using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public abstract class NodeSyntaxBase : ElementSyntaxBase, INodeSyntax
    {
        protected NodeSyntaxBase(IItemSyntax childItem, IEnumerable<ISectionSyntax> sections)
        {
            Sections = sections;
            ChildItem = childItem;
        }

        public IItemSyntax ChildItem { get; private set; }
        public IEnumerable<ISectionSyntax> Sections { get; private set; }
    }
}
