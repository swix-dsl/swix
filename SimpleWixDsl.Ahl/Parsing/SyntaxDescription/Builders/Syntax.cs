using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public static class Syntax
    {
        public static ItemBuilder Item()
        {
            return new ItemBuilder();
        }

        public static SectionBuilder Section(string keyword)
        {
            return new SectionBuilder(keyword);
        }
        
        public class ItemBuilder
        {
            private readonly List<string> _attributeNames;
            private readonly List<ISectionSyntax> _sectionSyntaxes;
            private IItemSyntax _itemSyntax;
            private EventHandler<ParseProcessEventArgs> _whenParsingStarts;
            private EventHandler<ParseProcessEventArgs> _whenParsingDone;

            internal ItemBuilder()
            {
                _attributeNames = new List<string>();
                _sectionSyntaxes = new List<ISectionSyntax>();
            }

            public ItemBuilder Attribute(string attributeName)
            {
                _attributeNames.Add(attributeName);
                return this;
            }

            public ItemBuilder Children(IItemSyntax childrenSyntax)
            {
                if (_itemSyntax != null)
                    throw new InvalidOperationException("Children syntax already specified");
                _itemSyntax = childrenSyntax;
                return this;
            }

            public ItemBuilder WhenStarts(Action<ParsedItem> whenParsingStarts)
            {
                if (_whenParsingStarts != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingStarts = (s, e) => whenParsingStarts((ParsedItem) e.Target);
                return this;
            }

            public ItemBuilder WhenDone(Action<ParsedItem> whenParsingDone)
            {
                if (_whenParsingDone != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingDone = (s, e) => whenParsingDone((ParsedItem) e.Target);
                return this;
            }

            public ItemBuilder Subsection(ISectionSyntax section)
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

        public class SectionBuilder
        {
            private readonly string _keyword;
            private readonly List<ISectionSyntax> _sectionSyntaxes;
            private IItemSyntax _itemSyntax;
            private EventHandler<ParseProcessEventArgs> _whenParsingStarts;
            private EventHandler<ParseProcessEventArgs> _whenParsingDone;

            internal SectionBuilder(string keyword)
            {
                _keyword = keyword;
                _sectionSyntaxes = new List<ISectionSyntax>();
            }

            public SectionBuilder Children(IItemSyntax childrenSyntax)
            {
                if (_itemSyntax != null)
                    throw new InvalidOperationException("Children syntax already specified");
                _itemSyntax = childrenSyntax;
                return this;
            }

            public SectionBuilder Subsection(ISectionSyntax section)
            {
                _sectionSyntaxes.Add(section);
                return this;
            }

            public SectionBuilder WhenStarts(Action<ParsedSection> whenParsingStarts)
            {
                if (_whenParsingStarts != null)
                    throw new InvalidOperationException("WhenDone action already specified");
                _whenParsingStarts = (s, e) => whenParsingStarts((ParsedSection) e.Target);
                return this;
            }

            public SectionBuilder WhenDone(Action<ParsedSection> whenParsingDone)
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