using System;

namespace SimpleWixDsl.Ahl
{
    [Serializable]
    public class IndentationException : LexerException
    {
        public IndentationException(int lineNumber, string message)
            : base(lineNumber, message)
        {
        }
    }
}