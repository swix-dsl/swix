namespace SimpleWixDsl.Ahl.Parsing
{
    public class SectionSyntax : SyntaxBase, ISectionSyntax
    {
        public SectionSyntax(string keyword,
                             IItemSyntax itemSyntax,
                             params ISectionSyntax[] subsections) 
            : base(itemSyntax, subsections)
        {
            Keyword = keyword;
        }

        public string Keyword { get; private set; }
    }
}