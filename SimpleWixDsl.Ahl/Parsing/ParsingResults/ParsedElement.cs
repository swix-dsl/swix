namespace SimpleWixDsl.Ahl.Parsing
{
    public abstract class ParsedElement
    {
        protected ParsedElement(SourceLocation startLocation, IElementSyntax syntax)
        {
            Syntax = syntax;
            StartLocation = startLocation;
        }

        public SourceLocation StartLocation { get; private set; }
        public IElementSyntax Syntax { get; private set; }
    }
}