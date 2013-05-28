namespace SimpleWixDsl.Swix.Parsing
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