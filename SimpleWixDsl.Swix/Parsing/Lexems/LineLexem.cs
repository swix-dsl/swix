using System.Collections.Generic;

namespace SimpleWixDsl.Swix.Parsing
{
    public class LineLexem : LexemBase
    {
        public LineLexem(SourceLocation location, LineType type, string key, int indentSize)
            :base(location)
        {
            IndentSize = indentSize;
            Attributes = new List<AttributeLexem>();
            Key = key;
            Type = type;
        }

        public LineType Type { get; private set; }
        public string Key { get; private set; }
        public int IndentSize { get; private set; }

        // should be immutable after creation and initial fill up :)
        public List<AttributeLexem> Attributes { get; private set; }
    }
}