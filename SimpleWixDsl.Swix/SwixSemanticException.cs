using System;

namespace SimpleWixDsl.Swix
{
    public class SwixSemanticException : Exception
    {
        private readonly int _lineNumber;

        public SwixSemanticException(int lineNumber, string message) : base(message)
        {
            _lineNumber = lineNumber;
        }

        public SwixSemanticException(int lineNumber, string message, Exception inner)
            : base(message, inner)
        {
            _lineNumber = lineNumber;
        }

        public int LineNumber
        {
            get { return _lineNumber; }
        }
    }
}