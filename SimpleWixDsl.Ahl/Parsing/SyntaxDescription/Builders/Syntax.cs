using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public static class Syntax
    {
        public class Item
        {
            private readonly List<string> _attributeNames;
            private readonly List<ISectionSyntax> _sectionSyntaxes;
            private IItemSyntax _itemSyntax;
            private EventHandler<ParseProcessEventArgs> _whenParsingStarts;
            private EventHandler<ParseProcessEventArgs> _whenParsingDone;

            public static Item Add()
            {
                return new Item();
            }

            private Item()
            {
                _attributeNames = new List<string>();
                _sectionSyntaxes = new List<ISectionSyntax>();
            }

            public Item Attribute(string attributeName)
            {
                _attributeNames.Add(attributeName);
                return this;
            }

            public Item Children(IItemSyntax childrenSyntax)
            {
                if (_itemSyntax != null)
                    throw new InvalidOperationException("Children syntax already specified");
                _itemSyntax = childrenSyntax;
                return this;
            }

            public Item WhenStarts(Action<ParsedItem> whenParsingStarts)
            {
                if (_whenParsingStarts != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingStarts = (s, e) => whenParsingStarts((ParsedItem) e.Target);
                return this;
            }

            public Item WhenDone(Action<ParsedItem> whenParsingDone)
            {
                if (_whenParsingDone != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingDone = (s, e) => whenParsingDone((ParsedItem) e.Target);
                return this;
            }

            public Item Subsection(ISectionSyntax section)
            {
                _sectionSyntaxes.Add(section);
                return this;
            }

            public IItemSyntax Make()
            {
                var itemSyntax = new ItemSyntax(_attributeNames, _itemSyntax, _sectionSyntaxes.ToArray());
                if (_whenParsingStarts != null)
                    itemSyntax.ParseStarted += _whenParsingStarts;
                if (_whenParsingDone != null)
                    itemSyntax.ParseFinished += _whenParsingDone;
                return itemSyntax;
            }

            public IItemSyntax MakeRecursive()
            {
                if (_itemSyntax != null)
                    throw new InvalidOperationException("Children syntax already specified");

                var itemSyntax = new RecursiveItemSyntax(_attributeNames);
                if (_whenParsingStarts != null)
                    itemSyntax.ParseStarted += _whenParsingStarts;
                if (_whenParsingDone != null)
                    itemSyntax.ParseFinished += _whenParsingDone;
                return itemSyntax;
            }
        }

        public class Section
        {
            private readonly string _keyword;
            private readonly List<ISectionSyntax> _sectionSyntaxes;
            private IItemSyntax _itemSyntax;
            private EventHandler<ParseProcessEventArgs> _whenParsingStarts;
            private EventHandler<ParseProcessEventArgs> _whenParsingDone;

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

            public Section WhenStarts(Action<ParsedSection> whenParsingStarts)
            {
                if (_whenParsingStarts != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingStarts = (s, e) => whenParsingStarts((ParsedSection) e.Target);
                return this;
            }

            public Section WhenDone(Action<ParsedSection> whenParsingDone)
            {
                if (_whenParsingDone != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingDone = (s, e) => whenParsingDone((ParsedSection) e.Target);
                return this;
            }

            public ISectionSyntax Make()
            {
                var sectionSyntax = new SectionSyntax(_keyword, _itemSyntax, _sectionSyntaxes.ToArray());
                if (_whenParsingStarts != null)
                    sectionSyntax.ParseStarted += _whenParsingStarts;
                if (_whenParsingDone != null)
                    sectionSyntax.ParseFinished += _whenParsingDone;
                return sectionSyntax;
            }
        }
    }
}