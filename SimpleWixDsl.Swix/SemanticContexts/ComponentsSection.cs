using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentsSection : BaseSwixSemanticContext
    {
        private readonly List<WixComponent> _components;
        private readonly List<WixComponent> _toAdd;

        public ComponentsSection(int line, IAttributeContext attributeContext, List<WixComponent> components)
            : base(line, attributeContext)
        {
            _components = components;
            _toAdd = new List<WixComponent>();
        }

        [ItemHandler]
        public ISemanticContext Component(string key, IAttributeContext itemContext)
        {
            try
            {
                var component = WixComponent.FromContext(key, itemContext);
                var itemSemanticContext = new ComponentItem(CurrentLine, CurrentAttributeContext, component);
                itemSemanticContext.OnFinished += (s, e) => _toAdd.Add(component);
                return itemSemanticContext;
            }
            catch (SwixSemanticException e)
            {
                throw new SwixSemanticException(FormatError(e.Message));
            }
        }

        [MetaHandler("harvest")]
        public ISemanticContext HandleMetaHarvest(string key, IAttributeContext metaContext)
        {
            return new StubSwixElement(CurrentLine, null, () =>
            {
                if (!Directory.Exists(key))
                    throw new SwixSemanticException(FormatError("Directory '" + key + "' does not exists"));
                var attrs = metaContext.GetDirectlySetAttributes();
                string filter;
                if (!attrs.TryGetValue("filter", out filter))
                    filter = "*.*";

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
                        throw new SwixSemanticException(FormatError("Invalid Regex pattern in excludePathRegex argument: {0}", e.ToString()));
                    }
                }

                bool withSubfolders = attrs.ContainsKey("withSubfolders") && attrs["withSubfolders"] == "yes";

                key = Path.GetFullPath(key); // eliminates relative parts like a\b\..\b2\c
                var files = Directory.GetFiles(key, filter, withSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (var filepath in files)
                {
                    int harvestingRootSubstringLength = key.Length;
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
                    catch (SwixSemanticException e)
                    {
                        throw new SwixSemanticException(FormatError("During harvesting of {0} file, exception occurs:\n{1}", filepath, e.ToString()));
                    }
                }
            });
        }

        protected override void FinishItemCore()
        {
            _components.AddRange(_toAdd);
        }
    }
}