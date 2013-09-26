using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class HarvestSemanticContext : ComponentsSection
    {
        private readonly string _folder;
        private readonly IDictionary<string, string> _directlySetAttrsibutes;

        public HarvestSemanticContext(int line, string folder, IAttributeContext context, List<WixComponent> components)
            : base(line, AddFromFolder(context, folder), components)
        {
            _folder = folder;
            _directlySetAttrsibutes = CurrentAttributeContext.GetDirectlySetAttributes();
        }

        private static IAttributeContext AddFromFolder(IAttributeContext attributeContext, string folder)
        {
            if (!attributeContext.GetDirectlySetAttributes().ContainsKey("from"))
            {
                // we imitate that harvest's key is also treated as 'from' specification, thus making
                // nested components searched by default in the directory being harvested
                attributeContext.SetAttributes(new[] {new AhlAttribute("from", folder)});
            }
            return attributeContext;
        }

        [ItemHandler]
        public override ISemanticContext Component(string key, IAttributeContext itemContext)
        {
            // we can't allow here non-rooted paths or paths with wix variables, because in this case
            // we won't be able to identify which harvested component we should replace with manually
            // specified one.
            // So, here is the check that SourcePath of resulting component is full, such file exists etc
            var fullPath = WixComponent.GetFullSourcePath(key, itemContext);
            var invalidPathChars = Path.GetInvalidPathChars();
            if (fullPath.IndexOfAny(invalidPathChars) != -1)
                throw new SwixSemanticException(CurrentLine, String.Format("Components inside ?harvest meta should reference existing files without WIX variables."));
            if (!File.Exists(fullPath))
                throw new SwixSemanticException(CurrentLine, String.Format("File {0} is not found. Components inside ?harvest meta should reference existing files without WIX variables.", fullPath));
            return base.Component(key, itemContext);
        }

        protected override void FinishItemCore()
        {
            if (!Directory.Exists(_folder))
                throw new SwixSemanticException(CurrentLine, string.Format("Directory '{0}' does not exists", _folder));

            var excludePathRegex = PrepareExcludeRegex();

            string filter;
            if (!_directlySetAttrsibutes.TryGetValue("filter", out filter))
                filter = "*.*";

            bool withSubfolders = _directlySetAttrsibutes.ContainsKey("withSubfolders") && _directlySetAttrsibutes["withSubfolders"] == "yes";

            var manuallySpecifiedSourcePaths = new HashSet<string>(GatheredComponents.Select(c => Path.GetFullPath(c.SourcePath)));
            var harvestedComponents = new List<WixComponent>();
            var folder = Path.GetFullPath(_folder); // eliminates relative parts like a\b\..\b2\c
            var files = Directory.GetFiles(folder, filter, withSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var filepath in files)
            {
                if (manuallySpecifiedSourcePaths.Contains(filepath))
                    continue;
                int harvestingRootSubstringLength = folder.Length;
                if (filepath[harvestingRootSubstringLength] == '\\')
                    harvestingRootSubstringLength++;
                var relativeToHarvestingRoot = filepath.Substring(harvestingRootSubstringLength);
                if (excludePathRegex.IsMatch(relativeToHarvestingRoot))
                    continue;
                var relativeDir = Path.GetDirectoryName(relativeToHarvestingRoot);
                try
                {
                    var component = WixComponent.FromContext(filepath, CurrentAttributeContext);
                    if (relativeDir != null)
                    {
                        component.TargetDir = component.TargetDir == null
                            ? relativeDir
                            : Path.Combine(component.TargetDir, relativeDir);
                    }
                    harvestedComponents.Add(component);
                }
                catch (SwixItemParsingException e)
                {
                    throw new SwixSemanticException(CurrentLine, string.Format("During harvesting of {0} file, exception occurs:\n{1}", new[] {filepath, e.ToString()}));
                }
            }
            GatheredComponents.AddRange(harvestedComponents);

            base.FinishItemCore();
        }

        private Regex PrepareExcludeRegex()
        {
            var attrs = CurrentAttributeContext.GetDirectlySetAttributes();
            Regex excludePathRegex;
            string excludePathRegexStr;
            if (!attrs.TryGetValue("excludePathRegex", out excludePathRegexStr))
            {
                excludePathRegex = new Regex("a^", RegexOptions.Compiled); // will match nothing
            }
            else
            {
                try
                {
                    excludePathRegex = new Regex(excludePathRegexStr, RegexOptions.Compiled);
                }
                catch (ArgumentException e)
                {
                    throw new SwixSemanticException(CurrentLine, string.Format("Invalid Regex pattern in excludePathRegex argument: {0}", new[] {e.ToString()}));
                }
            }
            return excludePathRegex;
        }
    }
}