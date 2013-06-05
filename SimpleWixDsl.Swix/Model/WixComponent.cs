using System.IO;

namespace SimpleWixDsl.Swix
{
    public class WixComponent
    {
        public static WixComponent FromContext(string key, IAttributeContext context)
        {
            var from = context.GetInheritedAttribute("from");
            var path = from == null ? key : Path.Combine(from, key);
            var result = new WixComponent(path);

            var targetDirRef = context.GetInheritedAttribute("targetDirRef");
            if (targetDirRef != null)
                result.TargetDirRef = targetDirRef;
            else
                throw new SwixSemanticException("targetDirRef attribute is mandatory for all components");

            var cabFileRef = context.GetInheritedAttribute("cabFileRef");
            if (cabFileRef != null)
                result.CabFileRef = cabFileRef;
            else
                throw new SwixSemanticException("cabFileRef attribute is mandatory for all components");

            var id = context.GetInheritedAttribute("id");
            if (id != null)
                result.Id = id;

            return result;
        }

        /// <summary>
        /// Source directory, combined with 'from' property from context if exists
        /// </summary>
        public string SourcePath { get; set; }

        public string TargetDirRef { get; set; }

        public string CabFileRef { get; set; }

        public string Id { get; set; }

        public WixComponent(string sourcePath)
        {
            SourcePath = sourcePath;
        }
    }
}