using System.Collections.Generic;
using System.IO;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class SwixParser
    {
        public static SwixModel Parse(TextReader sourceStream, IDictionary<string, string> variableDefinitions = null)
        {
            return new SwixParser(sourceStream).Run(variableDefinitions);
        }

        private readonly TextReader _sourceStream;

        private SwixParser(TextReader sourceStream)
        {
            _sourceStream = sourceStream;
        }

        private SwixModel Run(IDictionary<string, string> variableDefinitions)
        {
            var result = new SwixModel();
            var semanticContext = new FileSemanticContext(result);
            if (variableDefinitions != null)
                semanticContext.SetPredefinedSwixVariables(variableDefinitions);
            IParsingContext parsingContext = new ParsingContext(semanticContext);
            var lexer = new AhlLexer(parsingContext, _sourceStream);
            lexer.Run();
            return result;
        }
    }
}