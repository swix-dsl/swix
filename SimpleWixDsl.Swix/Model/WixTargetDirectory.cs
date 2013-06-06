using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class WixTargetDirectory
    {
        public static WixTargetDirectory FromAttributes(string key, IAttributeContext attributeContext, WixTargetDirectory parent)
        {
            var result = new WixTargetDirectory(key, parent);
            var id = attributeContext.GetInheritedAttribute("id");
            if (id != null)
                result.Id = id;
            return result;
        }

        public WixTargetDirectory(string name, WixTargetDirectory parent)
        {
            Name = name;
            Parent = parent;
            Subdirectories = new List<WixTargetDirectory>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public WixTargetDirectory Parent { get; set; }

        public List<WixTargetDirectory> Subdirectories { get; private set; }
    }
}