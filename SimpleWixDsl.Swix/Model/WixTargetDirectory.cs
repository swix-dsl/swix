using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class WixTargetDirectory
    {
        public static WixTargetDirectory FromAttributes(string key, IAttributeContext attributeContext)
        {
            var result = new WixTargetDirectory(key);
            var id = attributeContext.GetInheritedAttribute("id");
            if (id != null)
                result.Id = id;
            return result;
        }

        public WixTargetDirectory(string name)
        {
            Id = Name = name;
            Subdirectories = new List<WixTargetDirectory>();
        }

        public string Id { get; set; }
        public string Name { get; set; }

        public List<WixTargetDirectory> Subdirectories { get; private set; }
    }
}