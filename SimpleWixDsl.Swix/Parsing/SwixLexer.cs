using System.IO;

namespace SimpleWixDsl.Swix.Parsing
{
    public interface ISwixLexer
    {
    }

    public class SwixLexer : ISwixLexer
    {
        private readonly StreamReader _source;

        public SwixLexer(StreamReader source)
        {
            _source = source;
        }
    }
}