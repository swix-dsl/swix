using SimpleWixDsl.Swix.Parsing.SyntaxDescription;

namespace SimpleWixDsl.Swix.Parsing
{
    public class SwixSyntaxProvider
    {
         public SectionSyntax GetRootSyntax()
         {
             var componentGroups = new SectionSyntax("componentGroups", new ItemSyntax());
             var directories = new SectionSyntax("directories", new ItemSyntax());

             return new SectionSyntax("root",
                 componentGroups
                 );
         }
    }
}