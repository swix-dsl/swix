using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleWixDsl.Swix
{
    public class HarvestSemanticContext : BaseSwixSemanticContext
    {
        private readonly string _folder;
        private readonly List<WixComponent> _components;
        private readonly List<WixComponent> _toAdd;
        private readonly IDictionary<string, string> _directlySetAttrsibutes;

        public HarvestSemanticContext(int line, string folder, IAttributeContext context, List<WixComponent> components)
            : base(line, context)
        {
            _folder = folder;
            _components = components;
            _toAdd = new List<WixComponent>();
            _directlySetAttrsibutes = CurrentAttributeContext.GetDirectlySetAttributes();
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

            var folder = Path.GetFullPath(_folder); // eliminates relative parts like a\b\..\b2\c
            var files = Directory.GetFiles(folder, filter, withSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var filepath in files)
            {
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
                    _toAdd.Add(component);
                }
                catch (SwixItemParsingException e)
                {
                    throw new SwixSemanticException(CurrentLine, string.Format("During harvesting of {0} file, exception occurs:\n{1}", new[] { filepath, e.ToString() }));
                }
            }
            _components.AddRange(_toAdd);
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