using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public static class Syntax
    {
        public class Item
        {
            private readonly IAttributeSyntax _key;
            private readonly List<IAttributeSyntax> _attributes;
            private readonly List<ISectionSyntax> _sectionSyntaxes;
            private IItemSyntax _itemSyntax;

            public static Item Keyed(IAttributeSyntax key)
            {
                return new Item(key);
            }

            public static Item Keyed(string keyAttributeName, Func<string, bool> validation = null)
            {
                return new Item(new AttributeSyntax(keyAttributeName, true, string.Empty, validation));
            }

            private Item(IAttributeSyntax key)
            {
                _key = key;
                _attributes = new List<IAttributeSyntax>();
                _sectionSyntaxes = new List<ISectionSyntax>();
            }

            public Item Attribute(IAttributeSyntax attribute)
            {
                _attributes.Add(attribute);
                return this;
            }

            public Item Children(IItemSyntax childrenSyntax)
            {
                if (_itemSyntax != null)
                    throw new InvalidOperationException("Children syntax already specified");
                _itemSyntax = childrenSyntax;
                return this;
            }

            public Item Subsection(ISectionSyntax section)
            {
                _sectionSyntaxes.Add(section);
                return this;
            }

            public IItemSyntax Make()
            {
                return new ItemSyntax(_key, _attributes, _itemSyntax, _sectionSyntaxes.ToArray());
            }
        }

        public class Section
        {
            private readonly string _keyword;
            private readonly List<ISectionSyntax> _sectionSyntaxes;
            private IItemSyntax _itemSyntax;

            public static Section Keyword(string keyword)
            {
                return new Section(keyword);
            }

            private Section(string keyword)
            {
                _keyword = keyword;
                _sectionSyntaxes = new List<ISectionSyntax>();
            }

            public Section Children(IItemSyntax childrenSyntax)
            {
                if (_itemSyntax != null)
                    throw new InvalidOperationException("Children syntax already specified");
                _itemSyntax = childrenSyntax;
                return this;
            }

            public Section Subsection(ISectionSyntax section)
            {
                _sectionSyntaxes.Add(section);
                return this;
            }

            public ISectionSyntax Make()
            {
                return new SectionSyntax(_keyword, _itemSyntax, _sectionSyntaxes.ToArray());
            }
        }
    }
}