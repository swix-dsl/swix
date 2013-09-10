using System;

namespace SimpleWixDsl.Swix
{
    public class SwixItemParsingException : Exception
    {
        public SwixItemParsingException(string message) : base(message)
        {
        }

        public SwixItemParsingException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}