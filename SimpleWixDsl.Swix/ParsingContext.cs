using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class ParsingContext : IParsingContext
    {
        private class StubParsingContext : IParsingContext
        {
            public int Indent
            {
                get { return -1; }
            }

            public IParsingContext PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<Attribute> attributes)
            {
                var msg = String.Format("Inconsistent indent at line {0}", lineNumber);
                throw new InvalidOperationException(msg);
            }
        }

        private readonly ISemanticContext _semanticContext;
        private readonly IParsingContext _parentContext;

        public ParsingContext(ISemanticContext semanticContext)
        {
            _semanticContext = semanticContext;
            _parentContext = new StubParsingContext();
        }

        private ParsingContext(IParsingContext parentContext, ISemanticContext semanticContext)
        {
            _semanticContext = semanticContext;
            _parentContext = parentContext;
            Indent = -1;
        }

        public int Indent { get; private set; }

        public IParsingContext PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<Attribute> attributes)
        {
            // one of these cases are possible:
            // 1. indent equals to one of parent's indents --> finish prev semantic context and pass line to parent
            // 2. indent equals to our indent --> finish prev semantic context and add this line to current one
            // 3. indent is greater than ours --> create new block & semantic context and pass line there
            
            // 1st case
            if (indent <= _parentContext.Indent)
            {
                _semanticContext.FinishItem();
                return _parentContext.PushLine(lineNumber, indent, keyword, key, attributes);
            }

            // first line in this context, save indent size
            if (Indent == -1)
            {
                Indent = indent;
                var semSubcontext = _semanticContext.AddChild(key, attributes);
                return new ParsingContext(this, semSubcontext);
            }

            if (indent < Indent)
            {
                var msg = String.Format("Inconsistent indentation at line {0}. Should be between <={1} or >={2}, but was {3}", lineNumber, _parentContext.Indent, Indent, indent);
                throw new InvalidOperationException(msg);
            }

            // 2nd case
            if (indent == Indent)
            {
                _semanticContext.FinishItem();
                var semSubcontext = _semanticContext.AddChild(key, attributes);
                return new ParsingContext(this, semSubcontext);
            }
            else
            {
                // 3rd case
                var semSubcontext = _semanticContext.AddChild(key, attributes);
                return new ParsingContext(this, semSubcontext);
            }
        }
    }
}