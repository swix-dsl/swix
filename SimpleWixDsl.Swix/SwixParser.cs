using System.Collections.Generic;
using System.IO;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class SwixParser
    {
        public static SwixModel Parse(TextReader sourceStream, GuidProvider guidProvider, IDictionary<string, string> variableDefinitions = null)
        {
            return new SwixParser(sourceStream, guidProvider).Run(variableDefinitions);
        }

        private readonly TextReader _sourceStream;
        private readonly GuidProvider _guidProvider;

        private SwixParser(TextReader sourceStream, GuidProvider guidProvider)
        {
            _sourceStream = sourceStream;
            _guidProvider = guidProvider;
        }

        private SwixModel Run(IDictionary<string, string> variableDefinitions)
        {
            var result = new SwixModel();
            var semanticContext = new FileSemanticContext(result, _guidProvider);
            if (variableDefinitions != null)
                semanticContext.SetPredefinedSwixVariables(variableDefinitions);
            IParsingContext parsingContext = new ParsingContext(semanticContext);
            var lexer = new AhlLexer(parsingContext, _sourceStream);
            lexer.Run();
            return result;
        }
    }
}
