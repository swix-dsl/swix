namespace SimpleWixDsl.Ahl.Parsing
{
    public class SourceLocation
    {
        public SourceLocation(string filename, int sourceLine, int sourceColumn)
        {
            Filename = filename;
            SourceColumn = sourceColumn;
            SourceLine = sourceLine;
        }

        public string Filename { get; private set; }
        public int SourceLine { get; private set; }
        public int SourceColumn { get; private set; }
    }
}