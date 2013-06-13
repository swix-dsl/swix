using System.Collections.Generic;
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

            var fileName = context.GetInheritedAttribute("name");
            if (fileName != null)
                result.FileName = fileName;
            else
            {
                var idx = key.LastIndexOf('\\');
                result.FileName = key.Substring(idx + 1);
            }

            var targetDirRef = context.GetInheritedAttribute("targetDirRef");
            if (targetDirRef != null)
                result.TargetDirRef = targetDirRef;
            else
                throw new SwixSemanticException("targetDirRef attribute is mandatory for all components");

            var targetDir = context.GetInheritedAttribute("targetDir");
            if (targetDir != null)
                result.TargetDir = targetDir;

            var cabFileRef = context.GetInheritedAttribute("cabFileRef");
            if (cabFileRef != null)
                result.CabFileRef = cabFileRef;
            else
                throw new SwixSemanticException("cabFileRef attribute is mandatory for all components");

            var componentGroupRef = context.GetInheritedAttribute("componentGroupRef");
            if (componentGroupRef != null)
                result.ComponentGroupRef = componentGroupRef;

            var id = context.GetInheritedAttribute("id");
            if (id != null)
                result.Id = id;

            return result;
        }

        /// <summary>
        /// Source directory, combined with 'from' property from context if exists
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Allows to declare dir inline without previous declaration in :directories section.
        /// Will be created under specified TargetDirRef which is still mandatory.
        /// Separate directory entry with unique ID will be created in the directory structure 
        /// automatically. 
        /// </summary>
        public string TargetDir { get; set; }

        public string TargetDirRef { get; set; }

        public string CabFileRef { get; set; }
        
        public string ComponentGroupRef { get; set; }

        public string Id { get; set; }

        public string FileName { get; set; }

        public List<Shortcut> Shortcuts { get; set; }

        public WixComponent(string sourcePath)
        {
            SourcePath = sourcePath;
            Shortcuts = new List<Shortcut>();
        }
    }
}