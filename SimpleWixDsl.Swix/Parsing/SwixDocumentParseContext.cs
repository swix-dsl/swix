using System.Collections.Generic;
using SimpleWixDsl.Ahl.Parsing;
using SimpleWixDsl.Swix.Model;

namespace SimpleWixDsl.Swix.Parsing
{
    public class SwixDocumentParseContext
    {
        private readonly SwixModel _parseResult = new SwixModel();

        private readonly List<Dictionary<string, string>> _defaultAttributes = new List<Dictionary<string, string>>();

        private ISectionSyntax GetRootSyntax()
        {
            var componentGroups = Section("componentGroups").Children(Syntax.Item()
                                                                            .WhenDone(ProcessComponentGroup)
                                                                            .Make())
                                                            .Make();

            var directories = Section("directories").Children(Syntax.Item()
                                                                    .MakeRecursive())
                                                    .Make();
            return Syntax.Section("root")
                         .Subsection(componentGroups)
                         .Subsection(directories)
                         .Make();
        }

        private Syntax.SectionBuilder Section(string sectionName)
        {
            return Syntax.Section(sectionName)
                         .WhenStarts(section => _defaultAttributes.Add(section.DefaultAttributes))
                         .WhenDone(_ => _defaultAttributes.RemoveAt(_defaultAttributes.Count - 1));
        }

        private void ProcessComponentGroup(ParsedItem item)
        {
            var cg = new ComponentGroup(item.KeyValue);
            _parseResult.ComponentGroups.Add(cg);
        }

        private string GetAttributeValue(string attribute, ParsedItem item, string defaultValue)
        {
            string result;
            if (item.DirectAttributes.TryGetValue(attribute, out result))
                return result;

            for (int i = _defaultAttributes.Count - 1; i >= 0; i--)
            {
                var context = _defaultAttributes[i];
                if (context.TryGetValue(attribute, out result))
                    return result;
            }
            return defaultValue;
        }
    }
}