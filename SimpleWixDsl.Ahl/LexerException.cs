namespace SimpleWixDsl.Ahl
{
    public class LexerException : SourceCodeException
    {
        public LexerException(int lineNumber, string message)
            : base(lineNumber, message)
        {
        }
    }
}