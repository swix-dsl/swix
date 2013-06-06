using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class DirectoriesSection : BaseSwixSemanticContext
    {
        private readonly WixTargetDirectory _currentDir;
        private readonly List<WixTargetDirectory> _subdirs = new List<WixTargetDirectory>();

        public DirectoriesSection(IAttributeContext attributeContext, WixTargetDirectory currentDir) 
            : base(attributeContext)
        {
            _currentDir = currentDir;
        }

        [ItemHandler]
        public ISemanticContext RootDirectory(string key, IAttributeContext itemContext)
        {
            var dir = WixTargetDirectory.FromAttributes(key, itemContext, _currentDir);
            _subdirs.Add(dir);
            return new DirectoriesSection(CurrentAttributeContext, dir);
        }

        protected override void FinishItemCore()
        {
            _currentDir.Subdirectories.AddRange(_subdirs);
        }
    }
}