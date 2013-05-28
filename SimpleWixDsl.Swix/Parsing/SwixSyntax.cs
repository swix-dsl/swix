using SimpleWixDsl.Ahl.Parsing;

namespace SimpleWixDsl.Swix.Parsing
{
    public class SwixSyntaxProvider
    {
        public ISectionSyntax GetRootSyntax()
        {
            var componentGroups = Syntax.Section.Keyword("componentGroups")
                                        .Children(Syntax.Item.Keyed("Name").Make())
                                        .Make();
            var directories = Syntax.Section.Keyword("directories")
                                    .Children(Syntax.Item.Keyed("PartialPath", Validators.FileSystemPath).MakeRecursive())
                                    .Make();
            return Syntax.Section
                         .Keyword("root")
                         .Subsection(componentGroups)
                         .Subsection(directories)
                         .Make();
        }
    }
}