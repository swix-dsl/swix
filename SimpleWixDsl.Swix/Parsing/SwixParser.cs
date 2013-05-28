using SimpleWixDsl.Swix.Model;

namespace SimpleWixDsl.Swix.Parsing
{
    public class SwixParser
    {
        private readonly ISwixLexer _lexer;

        public SwixParser(ISwixLexer lexer)
        {
            _lexer = lexer;
        }

        public SwixModel Parse()
        {
            var result = new SwixModel();
            return result;
        }
    }
}