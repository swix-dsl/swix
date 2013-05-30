using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class ParsingContext : IParsingContext
    {
        private interface IParsingState
        {
            int Indent { get; }
            IParsingState PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes);
        }

        private class StubParsingState : IParsingState
        {
            public int Indent
            {
                get { return -1; }
            }

            public IParsingState PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes)
            {
                var msg = String.Format("Inconsistent indent at line {0}", lineNumber);
                throw new InvalidOperationException(msg);
            }
        }

        private class ParsingState : IParsingState
        {
            private readonly ISemanticContext _semanticContext;
            private readonly IParsingState _parentState;

            public ParsingState(ISemanticContext semanticContext)
            {
                _semanticContext = semanticContext;
                _parentState = new StubParsingState();
            }

            private ParsingState(IParsingState parentState, ISemanticContext semanticContext)
            {
                _semanticContext = semanticContext;
                _parentState = parentState;
                Indent = -1;
            }

            public int Indent { get; private set; }

            public IParsingState PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes)
            {
                // one of these cases are possible:
                // 1. indent equals to one of parent's indents --> finish prev semantic context and pass line to parent
                // 2. indent equals to our indent --> finish prev semantic context and add this line to current one
                // 3. indent is greater than ours --> create new block & semantic context and pass line there

                // 1st case
                if (indent <= _parentState.Indent)
                {
                    _semanticContext.FinishItem();
                    return _parentState.PushLine(lineNumber, indent, keyword, key, attributes);
                }

                // first line in this context, save indent size
                if (Indent == -1)
                {
                    Indent = indent;
                    var semSubcontext = _semanticContext.PushLine(lineNumber, keyword, key, attributes);
                    return new ParsingState(this, semSubcontext);
                }

                if (indent < Indent)
                {
                    var msg = String.Format("Inconsistent indentation at line {0}. Should be between <={1} or >={2}, but was {3}", lineNumber, _parentState.Indent, Indent, indent);
                    throw new InvalidOperationException(msg);
                }

                // 2nd case
                if (indent == Indent)
                {
                    _semanticContext.FinishItem();
                    var semSubcontext = _semanticContext.PushLine(lineNumber, keyword, key, attributes);
                    return new ParsingState(this, semSubcontext);
                }
                else
                {
                    // 3rd case
                    var semSubcontext = _semanticContext.PushLine(lineNumber, keyword, key, attributes);
                    return new ParsingState(this, semSubcontext);
                }
            }
        }

        private IParsingState _currentParsingState;

        public ParsingContext(ISemanticContext semanticContext)
        {
            _currentParsingState = new ParsingState(semanticContext);
        }

        public void PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes)
        {
            _currentParsingState = _currentParsingState.PushLine(lineNumber, indent, keyword, key, attributes);
        }
    }
}