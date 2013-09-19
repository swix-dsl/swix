using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class SwixSemanticException : SourceCodeException
    {
        public SwixSemanticException(int lineNumber, string message)
            : base(lineNumber, message)
        {
        }
    }
}