using System.IO;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class SwixParser
    {
        public static SwixModel Parse(TextReader sourceStream)
        {
            return new SwixParser(sourceStream).Run();
        }

        private readonly TextReader _sourceStream;

        private SwixParser(TextReader sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public SwixModel Run()
        {
            var result = new SwixModel();
            ISemanticContext semanticContext = new FileSemanticContext(result);
            IParsingContext parsingContext = new ParsingContext(semanticContext);
            var lexer = new AhlLexer(parsingContext, _sourceStream);
            lexer.Run();
            return result;
        }
    }
}