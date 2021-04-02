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
        private readonly IDictionary<string, string> _directlySetAttributes;

        public HarvestSemanticContext(int line, string folder, IAttributeContext context, List<WixComponent> components)
            : base(line, AddFromFolder(context, folder), components)
        {
            _folder = folder;
            _directlySetAttributes = CurrentAttributeContext.GetDirectlySetAttributes();
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
                throw new SwixSemanticException(CurrentLine, "Components inside ?harvest meta should reference existing files without WIX variables.");
            if (!File.Exists(fullPath))
                throw new SwixSemanticException(CurrentLine, $"File {fullPath} is not found. Components inside ?harvest meta should reference existing files without WIX variables.");
            return base.Component(key, itemContext);
        }

        protected override void FinishItemCore()
        {
            if (!Directory.Exists(_folder))
                throw new SwixSemanticException(CurrentLine, $"Directory '{_folder}' does not exists");

            var excludePathRegex = PrepareExcludeRegex();

            if (!_directlySetAttributes.TryGetValue("filter", out string filter))
                filter = "*.*";

            bool withSubfolders = _directlySetAttributes.ContainsKey("withSubfolders") && _directlySetAttributes["withSubfolders"] == "yes";

            var manuallySpecifiedSourcePaths = new HashSet<string>(GatheredComponents.Select(c => Path.GetFullPath(c.SourcePath)), StringComparer.OrdinalIgnoreCase);
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
                    throw new SwixSemanticException(CurrentLine, $"During harvesting of {filepath} file, exception occurs:\n{e}");
                }
            }

            GatheredComponents.AddRange(harvestedComponents);

            base.FinishItemCore();
        }

        private Regex PrepareExcludeRegex()
        {
            var attrs = CurrentAttributeContext.GetDirectlySetAttributes();
            Regex excludePathRegex;
            if (!attrs.TryGetValue("excludePathRegex", out string excludePathRegexStr))
            {
                excludePathRegex = new Regex("a^", RegexOptions.Compiled); // will match nothing
            }
            else
            {
                try
                {
                    excludePathRegex = new Regex(excludePathRegexStr, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
                catch (ArgumentException e)
                {
                    throw new SwixSemanticException(CurrentLine, $"Invalid Regex pattern in excludePathRegex argument: {e}");
                }
            }

            return excludePathRegex;
        }
    }
}
