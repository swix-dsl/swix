﻿using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Ahl
{
    public class ParsingContext : IParsingContext
    {
        private interface IParsingState
        {
            int Indent { get; }
            IParsingState PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes);
            IParsingState PushEof();
        }

        private class StubParsingState : IParsingState
        {
            public int Indent => -1;

            public IParsingState PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes)
                => throw new InvalidOperationException($"Inconsistent indent at line {lineNumber}");

            public IParsingState PushEof() => null;
        }

        private class ParsingState : IParsingState
        {
            private readonly ISemanticContext _semanticContext;
            private readonly IParsingState _parentState;

            public ParsingState(ISemanticContext semanticContext)
                : this(new StubParsingState(), semanticContext)
            {
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
                //
                // note that indent can't be greater than ours, when ours is not -1: as it is not -1 there was at
                // least one child, hence this ParsingState can't be on top of the states' stack: each time we add
                // item to it directly, we switch there immediately; if item is added to parent - this state is done
                // at all and replaced in stack with newly added item.
                // So with correct input we may happen here with Indent!=-1 only from child state via 1st case. But
                // then larger indent case already has been excluded by indent<Indent clause with exception.

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
                    var semSubContext = _semanticContext.PushLine(lineNumber, keyword, key, attributes);
                    return new ParsingState(this, semSubContext);
                }

                if (indent < Indent)
                {
                    var msg = $"Inconsistent indentation. Should be between <={_parentState.Indent} or >={Indent}, but was {indent}";
                    throw new IndentationException(lineNumber, msg);
                }

                // 2nd case
                if (indent == Indent)
                {
                    var semSubContext = _semanticContext.PushLine(lineNumber, keyword, key, attributes);
                    return new ParsingState(this, semSubContext);
                }

                throw new InvalidOperationException("This was not supposed to happen.");
            }

            public IParsingState PushEof()
            {
                _semanticContext.FinishItem();
                return _parentState;
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

        public void PushEof()
        {
            while (_currentParsingState != null)
            {
                _currentParsingState = _currentParsingState.PushEof();
            }
        }
    }
}
