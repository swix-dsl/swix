namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public class ItemSyntax : SyntaxBase
    {
        public ItemSyntax(ItemSyntax itemSyntax,
                          params SectionSyntax[] subsections)
            : base(itemSyntax, subsections)
        {
        }

        public ItemSyntax(params SectionSyntax[] subsections)
            : base(null, subsections)
        {
        }
    }
}
