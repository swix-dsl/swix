using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class ParsedItem : ParsedElement
    {
        public ParsedItem(SourceLocation startLocation,
                          IItemSyntax syntax,
                          string keyValue,
                          Dictionary<string, string> directAttributes,
                          List<ParsedSection> sections,
                          List<ParsedItem> items)
            : base(startLocation, syntax)
        {
            KeyValue = keyValue;
            Items = items;
            Sections = sections;
            DirectAttributes = directAttributes;
        }

        public new IItemSyntax Syntax
        {
            get { return (IItemSyntax) base.Syntax; }
        }

        public string KeyValue { get; private set; }
        public Dictionary<string, string> DirectAttributes { get; private set; }
        public List<ParsedSection> Sections { get; private set; }
        public List<ParsedItem> Items { get; private set; }
    }
}