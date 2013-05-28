using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class ParsedSection
    {
        public ParsedSection(SourceLocation startLocation,
                             ISectionSyntax syntax,
                             List<ParsedAttribute> defaultAttributes,
                             List<ParsedSection> sections,
                             List<ParsedItem> items)
        {
            Items = items;
            Sections = sections;
            DefaultAttributes = defaultAttributes;
            Syntax = syntax;
            StartLocation = startLocation;
        }

        public SourceLocation StartLocation { get; private set; }
        public ISectionSyntax Syntax { get; private set; }

        public List<ParsedAttribute> DefaultAttributes { get; private set; }
        public List<ParsedSection> Sections { get; private set; }
        public List<ParsedItem> Items { get; private set; }
    }
}