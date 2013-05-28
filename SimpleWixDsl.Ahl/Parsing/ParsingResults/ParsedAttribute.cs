namespace SimpleWixDsl.Ahl.Parsing
{
    public class ParsedAttribute
    {
        public ParsedAttribute(SourceLocation startLocation, IAttributeSyntax syntax, string value)
        {
            Value = value;
            Syntax = syntax;
            StartLocation = startLocation;
        }

        public SourceLocation StartLocation { get; private set; }
        public IAttributeSyntax Syntax { get; private set; }

        public string Value { get; private set; }
    }
}