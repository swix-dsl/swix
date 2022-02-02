using System.Collections.Generic;
using System.IO;

namespace SimpleWixDsl.Swix
{
    public class WixComponent
    {
        public static string GetFullSourcePath(string key, IAttributeContext context)
        {
            var fromBase = context.GetInheritedAttribute("fromBase");
            var from = context.GetInheritedAttribute("from");
            var path = @from == null ? key : Path.Combine(@from, key);

            if (fromBase != null)
                path = Path.Combine(fromBase, path);

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

            var moduleRef = context.GetInheritedAttribute("moduleRef");
            if (moduleRef != null)
                result.ModuleRef = moduleRef;

            if (cabFileRef == null && moduleRef == null)
                throw new SwixItemParsingException("cabFileRef or moduleRef attribute is mandatory for all components");

            if (cabFileRef != null && moduleRef != null)
                throw new SwixItemParsingException("You can't specify both cabFileRef and moduleRef for same component");

            result.OutputTag = context.GetInheritedAttribute("outputTag");
            result.Condition = context.GetInheritedAttribute("condition");

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

            var sddl = context.GetInheritedAttribute("sddl");
            if (sddl != null)
                result.Sddl = sddl;

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

        public string ModuleRef { get; set; }

        public string Condition { get; set; }

        public string ComponentGroupRef { get; set; }

        /// <summary>
        /// If set, the item will be present in the MSBuild transform output with metadata 'Tag' set
        /// to the specified value.
        /// </summary>
        public string OutputTag { get; set; }

        public string Id { get; set; }

        /// <summary>
        /// If set, Swix will generate PermissionEx element for the file with specified SDDL string.
        /// Effectively, it allows to set ACL permissions for a file.
        /// </summary>
        /// <remarks>
        /// To find the correct SDDL, one can set desired permissions on a test file and then from
        /// an elevated command line, run 'cacls.exe testfile.txt /s'. It will show file permissions
        /// as an SDDL string.
        /// </remarks>
        public string Sddl { get; set; }

        public string FileName { get; set; }

        public string MultiInstance { get; set; }

        public string Win64 { get; set; }

        public List<Shortcut> Shortcuts { get; set; }

        public List<Service> Services { get; set; }

        private WixComponent(string sourcePath)
        {
            SourcePath = sourcePath;
            Shortcuts = new List<Shortcut>();
            Services = new List<Service>();
        }
    }
}