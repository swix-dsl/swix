namespace SimpleWixDsl.Ahl.Parsing
{
    public abstract class LexemBase
    {
        protected LexemBase(SourceLocation location)
        {
            Location = location;
        }

        public SourceLocation Location { get; private set; }
    }
}