using System.Collections.Generic;
using SimpleWixDsl.Ahl.Parsing;
using SimpleWixDsl.Swix.Model;

namespace SimpleWixDsl.Swix.Parsing
{
    public class SwixDocumentParseContext
    {
        private List<Dictionary<string, string>> _defaultAttributes = new List<Dictionary<string, string>>();

        private ISectionSyntax GetRootSyntax()
        {
            var componentGroups = Syntax.Section.Keyword("componentGroups")
                                        .Children(Syntax.Item.Add()
                                                        .WhenDone(AddComponentGroup)
                                                        .Make())
                                        .Make();

            var directories = Syntax.Section.Keyword("directories")
                                    .Children(Syntax.Item.Add()
                                                    .MakeRecursive())
                                    .Make();
            return Syntax.Section
                         .Keyword("root")
                         .Subsection(componentGroups)
                         .Subsection(directories)
                         .Make();
        }

        private void AddComponentGroup(ParsedItem item)
        {
        }

        private string GetAttributeValue(string attribute, ParsedItem item)
        {
            //var q = item.DirectAttributes

            //for (int i = _defaultAttributes.Count - 1; i >= 0; i--)
            //{
            //    var context = _defaultAttributes[i];
            //}
            return null;
        }
    }
}