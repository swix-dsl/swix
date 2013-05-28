using System;

namespace SimpleWixDsl.Ahl.Parsing
{ 
    public class ElementSyntaxBase : IElementSyntaxCore
    {
        public event EventHandler<ParseProcessEventArgs> ParseStarted;

        public event EventHandler<ParseProcessEventArgs> ParseFinished;

        void IElementSyntaxCore.RaiseParseStarted(ParsedElement parsed)
        {
            var handler = ParseStarted;
            if (handler != null) handler(this, new ParseProcessEventArgs(parsed));
        }

        void IElementSyntaxCore.RaiseParseFinished(ParsedElement parsed)
        {
            var handler = ParseFinished;
            if (handler != null) handler(this, new ParseProcessEventArgs(parsed));
        }
    }
}