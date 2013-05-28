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
            var componentGroups = Syntax.Section("componentGroups")
                                        .Children(Syntax.Item()
                                                        .WhenDone(AddComponentGroup)
                                                        .Make())
                                        .Make();

            var directories = Syntax.Section("directories")
                                    .Children(Syntax.Item()
                                                    .MakeRecursive())
                                    .Make();
            return Syntax.Section("root")
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