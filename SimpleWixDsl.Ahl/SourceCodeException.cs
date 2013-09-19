using System;

namespace SimpleWixDsl.Ahl
{
    public class SourceCodeException : Exception
    {
        public int LineNumber { get; private set; }

        public SourceCodeException(int lineNumber, string message) 
            : base(message)
        {
            LineNumber = lineNumber;
        }
    }
}