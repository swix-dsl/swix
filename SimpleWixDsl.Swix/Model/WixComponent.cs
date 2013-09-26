using System.Collections.Generic;
using System.IO;

namespace SimpleWixDsl.Swix
{
    public class WixComponent
    {
        public static string GetFullSourcePath(string key, IAttributeContext context)
        {
            var from = context.GetInheritedAttribute("from");
            var path = @from == null ? key : Path.Combine(@from, key);
            return path;
        }

        public static WixComponent FromContext(string key, IAttributeContext context)
        {
            var result = new WixComponent(GetFullSourcePath(key, context));

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
                throw new SwixItemParsingException("targetDirRef attribute is mandatory for all components");

            var targetDir = context.GetInheritedAttribute("targetDir");
            if (targetDir != null)
                result.TargetDir = targetDir;

            var cabFileRef = context.GetInheritedAttribute("cabFileRef");
            if (cabFileRef != null)
                result.CabFileRef = cabFileRef;
            else
                throw new SwixItemParsingException("cabFileRef attribute is mandatory for all components");

            var componentGroupRef = context.GetInheritedAttribute("componentGroupRef");
            if (componentGroupRef != null)
                result.ComponentGroupRef = componentGroupRef;

            var multiInstance = context.GetInheritedAttribute("multiInstance");
            if (multiInstance != "yes" && multiInstance != "no" && multiInstance != null)
                throw new SwixItemParsingException("Optional 'multiInstance' attribute could be only 'yes' or 'no'");
            result.MultiInstance = multiInstance;

            var win64 = context.GetInheritedAttribute("win64");
            if (win64 != "yes" && win64 != "no" && win64 != null)
                throw new SwixItemParsingException("Optional 'win64' attribute could be only 'yes' or 'no'");
            result.Win64 = win64;

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

        public string MultiInstance { get; set; }

        public string Win64 { get; set; }

        public List<Shortcut> Shortcuts { get; set; }

        public List<Service> Services { get; set; }

        public WixComponent(string sourcePath)
        {
            SourcePath = sourcePath;
            Shortcuts = new List<Shortcut>();
            Services = new List<Service>();
        }
    }
}