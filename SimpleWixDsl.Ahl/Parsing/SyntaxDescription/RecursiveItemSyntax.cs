using System.Collections.Generic;
using System.Linq;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class RecursiveItemSyntax : IItemSyntax
    {
        public RecursiveItemSyntax(IAttributeSyntax key, IEnumerable<IAttributeSyntax> attributes)
        {
            Attributes = attributes;
            Key = key;
        }

        public IItemSyntax ChildItem
        {
            get { return this; }
        }

        public IEnumerable<ISectionSyntax> Sections
        {
            get { return Enumerable.Empty<ISectionSyntax>(); }
        }

        public IAttributeSyntax Key { get; private set; }
        public IEnumerable<IAttributeSyntax> Attributes { get; private set; }
    }
}