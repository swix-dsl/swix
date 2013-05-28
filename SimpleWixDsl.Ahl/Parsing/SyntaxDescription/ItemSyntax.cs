using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class ItemSyntax : SyntaxBase, IItemSyntax
    {
        public ItemSyntax(IAttributeSyntax key, IEnumerable<IAttributeSyntax> attributes, IItemSyntax itemSyntax, params ISectionSyntax[] subsections)
            : base(itemSyntax, subsections)
        {
            Attributes = attributes;
            Key = key;
        }

        public IAttributeSyntax Key { get; private set; }
        public IEnumerable<IAttributeSyntax> Attributes { get; private set; }
    }
}
