namespace SimpleWixDsl.Ahl.Parsing
{
    public class ParsedAttribute : ParsedElement
    {
        public ParsedAttribute(SourceLocation startLocation, IAttributeSyntax syntax, string value)
            : base(startLocation, syntax)
        {
            Value = value;
        }

        public new IAttributeSyntax Syntax
        {
            get { return (IAttributeSyntax) base.Syntax; }
        }

        public string Value { get; private set; }
    }
}