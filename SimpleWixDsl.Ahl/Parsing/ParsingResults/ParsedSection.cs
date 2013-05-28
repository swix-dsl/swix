﻿using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class ParsedSection : ParsedElement
    {
        public ParsedSection(SourceLocation startLocation,
                             ISectionSyntax syntax,
                             List<ParsedAttribute> defaultAttributes,
                             List<ParsedSection> sections,
                             List<ParsedItem> items)
            :base(startLocation, syntax)
        {
            Items = items;
            Sections = sections;
            DefaultAttributes = defaultAttributes;
        }

        public new ISectionSyntax Syntax { get { return (ISectionSyntax) base.Syntax; } }

        public List<ParsedAttribute> DefaultAttributes { get; private set; }
        public List<ParsedSection> Sections { get; private set; }
        public List<ParsedItem> Items { get; private set; }
    }
}