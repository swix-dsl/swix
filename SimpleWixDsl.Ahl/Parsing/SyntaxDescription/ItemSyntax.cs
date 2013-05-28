using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class ItemSyntax : NodeSyntaxBase, IItemSyntax
    {
        public ItemSyntax(IEnumerable<string> attributeNames, IItemSyntax itemSyntax, params ISectionSyntax[] subsections)
            : base(itemSyntax, subsections)
        {
            AttributeNames = attributeNames;
        }

        public IEnumerable<string> AttributeNames { get; private set; }
    }
}
