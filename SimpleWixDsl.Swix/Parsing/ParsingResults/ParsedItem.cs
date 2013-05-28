using System.Collections.Generic;
using SimpleWixDsl.Swix.Parsing.SyntaxDescription;

namespace SimpleWixDsl.Swix.Parsing
{
    public class ParsedItem
    {
        public ParsedItem(SourceLocation startLocation,
                          IItemSyntax syntax,
                          List<ParsedAttribute> directAttributes,
                          List<ParsedSection> sections,
                          List<ParsedItem> items)
        {
            Items = items;
            Sections = sections;
            DirectAttributes = directAttributes;
            Syntax = syntax;
            StartLocation = startLocation;
        }

        public SourceLocation StartLocation { get; private set; }
        public IItemSyntax Syntax { get; private set; }

        public List<ParsedAttribute> DirectAttributes { get; private set; }
        public List<ParsedSection> Sections { get; private set; }
        public List<ParsedItem> Items { get; private set; }
    }
}