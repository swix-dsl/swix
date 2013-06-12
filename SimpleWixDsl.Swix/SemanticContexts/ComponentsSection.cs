using System.Collections.Generic;
using System.IO;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentsSection : BaseSwixSemanticContext
    {
        private readonly List<WixComponent> _components;
        private readonly List<WixComponent> _toAdd;

        public ComponentsSection(IAttributeContext attributeContext, List<WixComponent> components)
            : base(attributeContext)
        {
            _components = components;
            _toAdd = new List<WixComponent>();
        }

        [ItemHandler]
        public ISemanticContext Component(string key, IAttributeContext itemContext)
        {
            return new StubSwixElement(CurrentAttributeContext, () =>
                {
                    try
                    {
                        var component = WixComponent.FromContext(key, itemContext);
                        _toAdd.Add(component);
                    }
                    catch (SwixSemanticException e)
                    {
                        throw new SwixSemanticException(FormatError(e.Message));
                    }
                });
        }

        [MetaHandler("harvest")]
        public ISemanticContext HandleMetaHarvest(string key, IAttributeContext metaContext)
        {
            return new StubSwixElement(null, () =>
                {
                    if (!Directory.Exists(key))
                        throw new SwixSemanticException(FormatError("Directory '" + key + "' does not exists"));
                    var attrs = metaContext.GetDirectlySetAttributes();
                    string filter;
                    if (!attrs.TryGetValue("filter", out filter))
                        filter = "*.*";
                    bool withSubfolders = attrs.ContainsKey("withSubfolders") && attrs["withSubfolders"] == "yes";

                    key = Path.GetFullPath(key); // eliminates relative parts like a\b\..\b2\c
                    var files = Directory.GetFiles(key, filter, withSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    foreach (var filepath in files)
                    {
                        int harvestingRootSubstringLength = key.Length;
                        if (filepath[harvestingRootSubstringLength] == '\\')
                            harvestingRootSubstringLength++;
                        var relativeToHarvestingRoot = filepath.Substring(harvestingRootSubstringLength);
                        var relativeDir = Path.GetDirectoryName(relativeToHarvestingRoot) ?? ".";
                        try
                        {
                            var component = WixComponent.FromContext(filepath, CurrentAttributeContext);
                            if (component.TargetDir == null)
                                component.TargetDir = relativeDir;
                            else
                                component.TargetDir = Path.Combine(component.TargetDir, relativeDir);
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