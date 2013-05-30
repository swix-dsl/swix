using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleWixDsl.Swix
{
    public class AhlLexer
    {
        private static readonly Regex LineRegex = new Regex(
            @"^
		       ( (?<keyword>[:!?][-A-Za-z_][-A-Za-z_0-9]*)  
		         (\ +(?<key>[^=\n\r!?: ,""]+|""(""""|[^""])*""))?
		       |
		         (?<key>[^=\n\r!?: ,""]+|""(""""|[^""])*"")
		       )  \ *
		       (::\ *(?<attrName>[-A-Za-z_][-A-Za-z_0-9]*)\ *=\ *(?<attrValue>[^=\n\r!?: ,""]+|""(""""|[^""])*"")  (\ *,\ *
		             (?<attrName>[-A-Za-z_][-A-Za-z_0-9]*)\ *=\ *(?<attrValue>[^=\n\r!?: ,""]+|""(""""|[^""])*""))*\ *)?$",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private readonly TextReader _sourceStream;
        private readonly IParsingContext _parsingContext;

        public AhlLexer(IParsingContext parsingContext, TextReader sourceStream)
        {
            _parsingContext = parsingContext;
            _sourceStream = sourceStream;
        }

        public void Run()
        {
            string line;
            int lineNumber = 0;
            while ((line = _sourceStream.ReadLine()) != null)
            {
                lineNumber++;
                if (line.Contains('\t'))
                    throw new LexerException(String.Format("Line {0} : AHL file cannot contain tabs", lineNumber));

                line = StripComments(line);

                int indent = 0;
                while (indent < line.Length && line[indent] == ' ')
                    indent++;

                // skip empty lines
                if (indent == line.Length) continue;

                line = line.Substring(indent);
                var match = LineRegex.Match(line);
                if (!match.Success)
                    throw new LexerException(String.Format("Line {0}: doesn't match AHL syntax:\n{1}", lineNumber, line));

                string keyword = match.Groups["keyword"].Success ? match.Groups["keyword"].Captures[0].Value : null;
                string key = match.Groups["key"].Success ? Unquote(match.Groups["key"].Captures[0].Value) : null;
                var attributes = new List<Attribute>();

                for (int i = 0; i < match.Groups["attrName"].Captures.Count; i++)
                {
                    var attrName = match.Groups["attrName"].Captures[i].Value;
                    var attrValue = Unquote(match.Groups["attrValue"].Captures[i].Value);
                    attributes.Add(new Attribute(attrName, attrValue));
                }

                _parsingContext.PushLine(lineNumber, indent, keyword, key, attributes);
            }
        }

        private string StripComments(string line)
        {
            int current = 0;
            while ((current = line.IndexOf("//", current, StringComparison.Ordinal)) != -1)
            {
                int pos = current;
                int quoteCount = line.Take(pos - 1).Count(c => c == '"');
                bool isPotentialCommentPartOfTheString = quoteCount % 2 == 1;
                if (!isPotentialCommentPartOfTheString)
                    return line.Substring(0, pos - 1);
                current += 2; // next time skip these '//' found now
            }
            return line;
        }

        private string Unquote(string value)
        {
            if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
            {
                // it is not a quoted string - just give it back
                return value;
            }
            return value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
        }
    }
}