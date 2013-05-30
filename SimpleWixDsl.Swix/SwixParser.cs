using System.Collections.Generic;
using System.IO;

namespace SimpleWixDsl.Swix
{
    public class SwixParser
    {
        public static SwixModel Parse(StreamReader sourceStream)
        {
            return new SwixParser(sourceStream).Run();
        }

        private readonly StreamReader _sourceStream;

        private SwixParser(StreamReader sourceStream)
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

    public class FileSemanticContext : ISemanticContext
    {
        public FileSemanticContext(SwixModel result)
        {
            throw new System.NotImplementedException();
        }

        public ISemanticContext AddChild(string key, IEnumerable<Attribute> attributes)
        {
            throw new System.NotImplementedException();
        }

        public void FinishItem()
        {
            throw new System.NotImplementedException();
        }
    }
}