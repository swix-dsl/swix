using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class DirectoriesSection : BaseSwixSemanticContext
    {
        private readonly WixTargetDirectory _currentDir;
        private readonly List<WixTargetDirectory> _subdirs = new List<WixTargetDirectory>();

        public DirectoriesSection(int line, IAttributeContext attributeContext, WixTargetDirectory currentDir)
            : base(line, attributeContext)
        {
            _currentDir = currentDir;
        }

        [ItemHandler]
        public ISemanticContext RootDirectory(string key, IAttributeContext itemContext)
        {
            try
            {
                var dir = WixTargetDirectory.FromAttributes(key, itemContext, _currentDir);
                _subdirs.Add(dir);
                return new DirectoriesSection(CurrentLine, CurrentAttributeContext, dir);
            }
            catch (SwixItemParsingException e)
            {
                throw new SwixSemanticException(CurrentLine, string.Format("{0}", new[] {e.Message}));
            }
        }

        [MetaHandler("makeCustomizable")]
        public ISemanticContext MakeCustomizable(string key, IAttributeContext attributes)
        {
            var publicWixPathPropertyName = key;
            var registryStorageKey = attributes.GetInheritedAttribute("regKey");
            var defaultValue = attributes.GetInheritedAttribute("defaultValue");
            if (key == null)
                throw new SwixSemanticException(CurrentLine, "Key attribute for ?makeCustomizable is mandatory and specifies property name with which user can customize the value");
            if (registryStorageKey == null)
                throw new SwixSemanticException(CurrentLine, "registryStorageKey attribute for ?makeCustomizable is mandatory");
            if (_currentDir.Customization != null)
                throw new SwixSemanticException(CurrentLine, "This directory already has customization assigned");
            WixTargetDirCustomization customization; 
            try
            {
                customization = new WixTargetDirCustomization(_currentDir, registryStorageKey, publicWixPathPropertyName);
            }
            catch (SwixItemParsingException e)
            {
                throw new SwixSemanticException(CurrentLine, e.Message);
            } 
            customization.DefaultValue = defaultValue;
            return new StubSwixElement(CurrentLine, attributes, () => _currentDir.Customization = customization);
        }

        protected override void FinishItemCore()
        {
            _currentDir.Subdirectories.AddRange(_subdirs);
        }
    }
}