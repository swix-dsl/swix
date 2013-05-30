using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public interface IParsingContext
    {
        int Indent { get; }
        IParsingContext PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<Attribute> attributes);
    }
}