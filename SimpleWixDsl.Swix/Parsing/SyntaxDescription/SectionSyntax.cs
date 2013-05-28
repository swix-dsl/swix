namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public class SectionSyntax : SyntaxBase
    {
        public SectionSyntax(string sectionKeyword,
                             ItemSyntax itemSyntax,
                             params SectionSyntax[] subsections) 
            : base(itemSyntax, subsections)
        {
        }

        public SectionSyntax(string sectionKeyword,
                             params SectionSyntax[] subsections)
            :base(null, subsections)
        {
        }
    }
}