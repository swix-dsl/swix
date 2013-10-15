using System;

namespace SimpleWixDsl.Swix
{
    public class SwixException : Exception
    {
        public SwixException(string message)
            : base(message)
        {
        }

        public SwixException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}