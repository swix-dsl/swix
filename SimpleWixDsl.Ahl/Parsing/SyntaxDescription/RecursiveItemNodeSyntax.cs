using System.Collections.Generic;
using System.Linq;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class RecursiveItemSyntax : ElementSyntaxBase, IItemSyntax
    {
        public RecursiveItemSyntax(IEnumerable<string> attributeNames)
        {
            AttributeNames = attributeNames;
        }

        public IItemSyntax ChildItem
        {
            get { return this; }
        }

        public IEnumerable<ISectionSyntax> Sections
        {
            get { return Enumerable.Empty<ISectionSyntax>(); }
        }

        public IEnumerable<string> AttributeNames { get; private set; }
    }
}