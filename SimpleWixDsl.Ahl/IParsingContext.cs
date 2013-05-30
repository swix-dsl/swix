using System.Collections.Generic;

namespace SimpleWixDsl.Ahl
{
    public interface IParsingContext
    {
        void PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<AhlAttribute> attributes);
        void PushEof();
    }
}