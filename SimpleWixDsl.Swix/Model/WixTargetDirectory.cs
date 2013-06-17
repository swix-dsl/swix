using System.Collections.Generic;
using System.Linq;

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

            var refOnly = attributeContext.GetInheritedAttribute("refOnly");
            if (refOnly != null && refOnly != "yes" && refOnly != "no")
                throw new SwixSemanticException("Attribute 'refOnly' should be either 'yes' or 'no'.");
            result.RefOnly = refOnly == "yes";

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
        public bool RefOnly { get; set; }

        public List<WixTargetDirectory> Subdirectories { get; private set; }

        public string GetFullTargetPath()
        {
            return string.Join("\\", GetParentSequence().Reverse());
        }

        private IEnumerable<string> GetParentSequence()
        {
            var current = this;
            while (current != null)
            {
                yield return current.Name;
                current = current.Parent;
            }
        }
    }
}