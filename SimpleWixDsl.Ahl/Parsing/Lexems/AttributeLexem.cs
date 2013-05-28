namespace SimpleWixDsl.Ahl.Parsing
{
    public class AttributeLexem : LexemBase
    {
        public AttributeLexem(SourceLocation location, string id, string value) 
            : base(location)
        {
            Value = value;
            Id = id;
        }

        public string Id { get; private set; }
        public string Value { get; private set; }
    }
}