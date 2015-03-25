using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SimpleWixDsl.Swix
{
    public class WxsGenerator
    {
        private class CabFileCounter
        {
            private readonly int _splitNumber;
            private int _current;

            public CabFileCounter(int startId, int splitNumber)
            {
                _current = 0;
                StartId = startId;
                _splitNumber = splitNumber;
            }

            public int StartId { get; private set; }

            public int GetNextId()
            {
                return StartId + _current++%_splitNumber;
            }
        }

        private const int MaxIdLength = 72;
        private readonly SwixModel _model;
        private readonly GuidProvider _guidProvider;
        private readonly Dictionary<string, CabFileCounter> _cabFileCounters = new Dictionary<string, CabFileCounter>();
        private readonly Dictionary<string, WixTargetDirectory> _directories = new Dictionary<string, WixTargetDirectory>();
        private HashSet<string> _nonUniqueDirectoryReadableIds;
        private Dictionary<string, HashSet<string>> _additionalComponentIdsByGroups = null;

        public WxsGenerator(SwixModel model, GuidProvider guidProvider)
        {
            _model = model;
            _guidProvider = guidProvider;

            VerifyDirectories();
            AssignCabFileIds();
            FindNonUniqueDirectoryReadableIds();
            AssignDirectoryIds();
            VerifyDirectoryRefs();
            VerifyCabFileRefs();
            HandleInlineTargetDirSpecifications();
        }

        private void VerifyDirectories()
        {
            foreach (var subdirectory in _model.RootDirectory.Subdirectories)
                VerifySubdirectoriesDontHaveRefOnlyAttributeSet(subdirectory);
            VerifyDirectoriesMarkedAsCreateOnInstallOrRemoveOnUninstallOrCustomizedHaveComponentGroupRefSet(_model.RootDirectory);
        }

        private void VerifySubdirectoriesDontHaveRefOnlyAttributeSet(WixTargetDirectory dir)
        {
            foreach (var subdir in dir.Subdirectories)
            {
                if (subdir.RefOnly)
                    throw new SwixSemanticException(0, string.Format("Directory {0} is marked as refOnly and has parent. refOnly dirs can be only top-level", subdir.GetFullTargetPath()));
                VerifySubdirectoriesDontHaveRefOnlyAttributeSet(subdir);
            }
        }

        private void VerifyDirectoriesMarkedAsCreateOnInstallOrRemoveOnUninstallOrCustomizedHaveComponentGroupRefSet(WixTargetDirectory root)
        {
            var invalidDirectories = TraverseDfs(root, d => d.Subdirectories)
                .Where(d => (d.RemoveOnUninstall || d.CreateOnInstall || d.Customization != null) && String.IsNullOrWhiteSpace(d.ComponentGroupRef))
                .Select(d => d.GetFullTargetPath())
                .ToArray();
            if (invalidDirectories.Any())
            {
                var dirList = String.Join(", ", invalidDirectories);
                throw new SwixSemanticException(0, string.Format("Directories {0} are customized or marked as createOnInstall/removeOnUninstall but don't have valid ComponentGroupRef assigned", dirList));
            }
        }

        private void AssignCabFileIds()
        {
            int id = _model.DiskIdStartFrom;
            foreach (var cabFile in _model.CabFiles)
            {
                _cabFileCounters[cabFile.Name] = new CabFileCounter(id, cabFile.Split);
                id += cabFile.Split;
            }
        }

        private void VerifyCabFileRefs()
        {
            foreach (var component in _model.Components.Where(c => c.CabFileRef != null))
            {
                if (!_cabFileCounters.ContainsKey(component.CabFileRef))
                    throw new SwixSemanticException(0, String.Format("Component {0} references cabFile {1} which was not declared", component.SourcePath, component.CabFileRef));
            }
        }

        private void VerifyDirectoryRefs()
        {
            foreach (var component in _model.Components)
            {
                VerifyTargetDirRef("Component", component.SourcePath, component.TargetDirRef);
                foreach (var shortcut in component.Shortcuts)
                    VerifyTargetDirRef("Shortcut", shortcut.Name, shortcut.TargetDirRef);
            }
        }

        private void VerifyTargetDirRef(string referencingEntityType, string referencingEntityName, string targetDirRef)
        {
            if (_nonUniqueDirectoryReadableIds.Contains(targetDirRef))
            {
                var msg = string.Format("{0} {1} references directory via implicit ID {2} which is not unique",
                    referencingEntityType,
                    referencingEntityName,
                    targetDirRef);
                throw new SwixSemanticException(0, msg);
            }
            if (!_directories.ContainsKey(targetDirRef))
            {
                var msg = string.Format("{0} {1} references undeclared directory ID {2}",
                    referencingEntityType,
                    referencingEntityName,
                    targetDirRef);
                throw new SwixSemanticException(0, msg);
            }
        }

        private void AssignDirectoryIds()
        {
            foreach (var dir in TraverseDfs(_model.RootDirectory, dir => dir.Subdirectories))
            {
                var id = dir.Id ?? MakeReadableId(dir.Name, MaxIdLength);
                if (_nonUniqueDirectoryReadableIds.Contains(id))
                {
                    AssignDirectoryUniqueId(dir);
                }
                else
                {
                    dir.Id = id;
                    _directories[id] = dir;
                }
            }
        }

        // modifies directory structure to include all directories specified inline and modifies components
        // to have direct targetDirRefs there instead of combination targetDirRef/targetDir
        private void HandleInlineTargetDirSpecifications()
        {
            foreach (var component in _model.Components)
            {
                if (component.TargetDir == string.Empty)
                    component.TargetDir = null;
                if (component.TargetDir != null)
                {
                    component.TargetDirRef = HandleInlineTargetDir(component.TargetDirRef, component.TargetDir);
                    component.TargetDir = null;
                }

                foreach (var shortcut in component.Shortcuts)
                {
                    if (shortcut.TargetDir == string.Empty)
                        shortcut.TargetDir = null;
                    if (shortcut.TargetDir != null)
                    {
                        shortcut.TargetDirRef = HandleInlineTargetDir(shortcut.TargetDirRef, shortcut.TargetDir);
                        shortcut.TargetDir = null;
                    }
                }
            }
        }

        private string HandleInlineTargetDir(string targetDirRef, string targetDir)
        {
            string[] path = targetDir.Split('\\');
            var dir = _directories[targetDirRef];
            foreach (var nextDirName in path)
            {
                var nextDir = dir.Subdirectories.Find(subdir => subdir.Name == nextDirName);
                if (nextDir == null)
                {
                    nextDir = new WixTargetDirectory(nextDirName, dir);
                    AssignDirectoryUniqueId(nextDir);
                    dir.Subdirectories.Add(nextDir);
                }
                dir = nextDir;
            }
            string resultingTargetDirRef = dir.Id;
            return resultingTargetDirRef;
        }

        private void FindNonUniqueDirectoryReadableIds()
        {
            _nonUniqueDirectoryReadableIds = new HashSet<string>();
            var usedIds = new HashSet<string>();
            foreach (var dir in TraverseDfs(_model.RootDirectory, dir => dir.Subdirectories))
            {
                var id = dir.Id ?? MakeReadableId(dir.Name, MaxIdLength);
                bool isUnique = usedIds.Add(id);
                if (!isUnique)
                    _nonUniqueDirectoryReadableIds.Add(id);
            }
        }

        private void AssignDirectoryUniqueId(WixTargetDirectory dir)
        {
            var readableId = dir.Id ?? MakeReadableId(dir.Name, MaxIdLength - 33);
            var guid = _guidProvider.Get(SwixGuidType.Directory, dir.GetFullTargetPath());
            var finalId = String.Format("{0}_{1:N}", readableId, guid);
            dir.Id = finalId;
            _directories[finalId] = dir;
        }

        public void WriteToStream(StreamWriter target)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Entitize;
            using (var doc = XmlWriter.Create(target, settings))
            {
                doc.WriteStartDocument();
                doc.WriteComment(@"
  This file is autogenerated. Do not make changes to it manually as it will be regenerated
  on the next build. Change source *.swix file instead.
");
                doc.WriteStartElement("Wix", "http://schemas.microsoft.com/wix/2006/wi");

                doc.WriteStartElement("Fragment");

                WriteSubDirectories(doc, _model.RootDirectory);
                WriteDirectoryCustomizations(doc, _model.RootDirectory);
                WriteCabFiles(doc, _model.CabFiles);
                WriteComponents(doc, _model.Components);
                WriteComponentGroups(doc);

                doc.WriteEndElement();

                doc.WriteEndElement();
                doc.WriteEndDocument();
                doc.Flush();
            }
        }

        private void WriteComponentGroups(XmlWriter doc)
        {
            if (_additionalComponentIdsByGroups == null)
                throw new InvalidOperationException("This method can't be executed until _additionalComponentIdsByGroups is filled");
            var groupedComponents = _model.Components.GroupBy(c => c.ComponentGroupRef);
            foreach (var group in groupedComponents)
            {
                var componentGroupRef = group.Key;
                doc.WriteStartElement("ComponentGroup");
                doc.WriteAttributeString("Id", componentGroupRef);

                foreach (var component in group)
                {
                    doc.WriteStartElement("ComponentRef");
                    doc.WriteAttributeString("Id", GetComponentId(component));
                    doc.WriteEndElement();
                }

                var removableDirectories = TraverseDfs(_model.RootDirectory, d => d.Subdirectories)
                    .Where(d => d.ComponentGroupRef == componentGroupRef && d.RemoveOnUninstall);
                foreach (var dir in removableDirectories)
                {
                    doc.WriteStartElement("ComponentRef");
                    doc.WriteAttributeString("Id", GetRemoveOnUninstallComponentId(dir));
                    doc.WriteEndElement();
                }

                var creatableDirectories = TraverseDfs(_model.RootDirectory, d => d.Subdirectories)
                    .Where(d => d.ComponentGroupRef == componentGroupRef && d.CreateOnInstall);
                foreach (var dir in creatableDirectories)
                {
                    doc.WriteStartElement("ComponentRef");
                    doc.WriteAttributeString("Id", GetCreateOnInstallComponentId(dir));
                    doc.WriteEndElement();
                }

                HashSet<string> additionalComponentIds;
                if (_additionalComponentIdsByGroups.TryGetValue(componentGroupRef, out additionalComponentIds))
                {
                    foreach (var additionalId in additionalComponentIds)
                    {
                        doc.WriteStartElement("ComponentRef");
                        doc.WriteAttributeString("Id", additionalId);
                        doc.WriteEndElement();
                    }
                }

                doc.WriteEndElement();
            }
        }

        private void WriteComponents(XmlWriter doc, IEnumerable<WixComponent> components)
        {
            WriteCreateOnInstallComponent(doc);
            WriteRemoveOnUninstallComponent(doc);
            var groupedByDirComponents = components.GroupBy(c => c.TargetDirRef);
            foreach (var group in groupedByDirComponents)
            {
                var targetDirRef = group.Key;
                doc.WriteStartElement("DirectoryRef");
                doc.WriteAttributeString("Id", targetDirRef);

                foreach (var component in group)
                    WriteComponent(doc, component);

                doc.WriteEndElement();
            }
        }

        private void WriteCreateOnInstallComponent(XmlWriter doc)
        {
            var createdDirs = TraverseDfs(_model.RootDirectory, d => d.Subdirectories)
                .Where(d => d.CreateOnInstall)
                .ToArray();

            if (!createdDirs.Any()) return;

            doc.WriteStartElement("DirectoryRef");
            doc.WriteAttributeString("Id", "TARGETDIR");

            foreach (var createdDir in createdDirs)
            {
                doc.WriteStartElement("Component");
                doc.WriteAttributeString("Id", GetCreateOnInstallComponentId(createdDir));
                doc.WriteAttributeString("Guid", GetCreateOnInstallComponentGuid(createdDir).ToString("B").ToUpperInvariant());
                doc.WriteAttributeString("KeyPath", "yes");
                if (createdDir.MultiInstance != null)
                    doc.WriteAttributeString("MultiInstance", createdDir.MultiInstance);

                doc.WriteStartElement("CreateFolder");
                doc.WriteAttributeString("Directory", createdDir.Id);
                doc.WriteEndElement();

                doc.WriteEndElement();
            }

            doc.WriteEndElement();
        }

        private void WriteRemoveOnUninstallComponent(XmlWriter doc)
        {
            var removedDirs = TraverseDfs(_model.RootDirectory, d => d.Subdirectories)
                .Where(d => d.RemoveOnUninstall)
                .ToArray();

            if (!removedDirs.Any()) return;

            doc.WriteStartElement("DirectoryRef");
            doc.WriteAttributeString("Id", "TARGETDIR");

            foreach (var removedDir in removedDirs)
            {
                doc.WriteStartElement("Component");
                doc.WriteAttributeString("Id", GetRemoveOnUninstallComponentId(removedDir));
                doc.WriteAttributeString("Guid", GetRemoveOnUninstallComponentGuid(removedDir).ToString("B").ToUpperInvariant());
                doc.WriteAttributeString("KeyPath", "yes");
                if (removedDir.MultiInstance != null)
                    doc.WriteAttributeString("MultiInstance", removedDir.MultiInstance);
                
                doc.WriteStartElement("RemoveFolder");
                doc.WriteAttributeString("Id", removedDir.Id);
                doc.WriteAttributeString("Directory", removedDir.Id);
                doc.WriteAttributeString("On", "uninstall");
                doc.WriteEndElement();

                doc.WriteEndElement();
            }

            doc.WriteEndElement();
        }

        private void WriteComponent(XmlWriter doc, WixComponent component)
        {
            doc.WriteStartElement("Component");
            var id = GetComponentId(component);
            doc.WriteAttributeString("Id", id);
            if (component.MultiInstance != null)
                doc.WriteAttributeString("MultiInstance", component.MultiInstance);
            if (component.Win64 != null)
                doc.WriteAttributeString("Win64", component.Win64);
            var componentGuid = _guidProvider.Get(SwixGuidType.Component, GetComponentFullTargetPath(component));
            doc.WriteAttributeString("Guid", componentGuid.ToString("B").ToUpperInvariant());

            if (component.Condition != null)
            {
                doc.WriteStartElement("Condition");
                doc.WriteCData(component.Condition);
                doc.WriteEndElement();
            }

            doc.WriteStartElement("File");
            doc.WriteAttributeString("Id", id);
            doc.WriteAttributeString("KeyPath", "yes");
            doc.WriteAttributeString("Source", component.SourcePath);
            doc.WriteAttributeString("Name", component.FileName);

            if (component.CabFileRef != null)
                doc.WriteAttributeString("DiskId", _cabFileCounters[component.CabFileRef].GetNextId().ToString(CultureInfo.InvariantCulture));

            WriteComponentShortcuts(doc, component);

            doc.WriteEndElement();

            WriteComponentServices(doc, component);

            doc.WriteEndElement();
        }

        private void WriteComponentShortcuts(XmlWriter doc, WixComponent component)
        {
            foreach (var shortcut in component.Shortcuts)
            {
                doc.WriteStartElement("Shortcut");
                var fullPath = string.Format("{0}\\{1}", _directories[shortcut.TargetDirRef].GetFullTargetPath(), shortcut.Name);
                var guid = _guidProvider.Get(SwixGuidType.Shortcut, fullPath);
                var id = MakeUniqueId(guid, shortcut.Name, MaxIdLength);
                doc.WriteAttributeString("Id", id);
                doc.WriteAttributeString("Name", shortcut.Name);
                if (shortcut.Args != null)
                    doc.WriteAttributeString("Arguments", shortcut.Args);
                if (shortcut.WorkingDir != null)
                    doc.WriteAttributeString("WorkingDirectory", shortcut.WorkingDir);
                doc.WriteAttributeString("Advertise", "yes");
                doc.WriteAttributeString("Directory", shortcut.TargetDirRef);
                doc.WriteEndElement();
            }
        }

        private void WriteComponentServices(XmlWriter doc, WixComponent component)
        {
            foreach (var service in component.Services)
            {
                doc.WriteStartElement("ServiceInstall");
                if (service.Id == null)
                {
                    var guid = _guidProvider.Get(SwixGuidType.ServiceInstall, service.Name);
                    service.Id = MakeUniqueId(guid, service.Name, MaxIdLength);
                }
                doc.WriteAttributeString("Id", service.Id);
                doc.WriteAttributeString("Name", service.Name);
                doc.WriteAttributeString("DisplayName", service.DisplayName ?? service.Name);
                if (service.Description != null)
                    doc.WriteAttributeString("Description", service.Description);
                
                var startupType = (service.Start != ServiceStartupType.Unset ? service.Start : ServiceStartupType.Auto).ToString();
                doc.WriteAttributeString("Start", ToCamelCase(startupType));

                var hostingType = (service.Type != ServiceHostingType.Unset ? service.Type : ServiceHostingType.OwnProcess).ToString();
                doc.WriteAttributeString("Type", ToCamelCase(hostingType));

                var errControl = (service.ErrorControl != ServiceErrorControl.Unset ? service.ErrorControl : ServiceErrorControl.Ignore).ToString();
                doc.WriteAttributeString("ErrorControl", ToCamelCase(errControl));

                if (service.Args != null)
                    doc.WriteAttributeString("Arguments", service.Args);

                if (service.Vital != null)
                    doc.WriteAttributeString("Vital", service.Vital);

                doc.WriteEndElement();
            }
        }

        private string ToCamelCase(string str)
        {
            if (char.IsLower(str[0]))
                return str;
            var sb = new StringBuilder(str);
            sb[0] = char.ToLower(str[0]);
            return sb.ToString();
        }

        private void WriteCabFiles(XmlWriter doc, IEnumerable<CabFile> cabFiles)
        {
            foreach (var cabFile in cabFiles)
            {
                var counter = _cabFileCounters[cabFile.Name];
                for (int i = 0; i < cabFile.Split; i++)
                {
                    var id = string.Format("{0}", counter.StartId + i);
                    var filename = string.Format("{0}_{1:D2}.cab", cabFile.Name, counter.StartId + i);
                    doc.WriteStartElement("Media");
                    doc.WriteAttributeString("Id", id);
                    doc.WriteAttributeString("Cabinet", filename);
                    doc.WriteAttributeString("EmbedCab", "yes");
                    doc.WriteAttributeString("CompressionLevel", cabFile.CompressionLevel);
                    doc.WriteEndElement();
                }
            }
        }

        private void WriteSubDirectories(XmlWriter doc, WixTargetDirectory rootDirectory)
        {
            foreach (var directory in rootDirectory.Subdirectories)
            {
                doc.WriteStartElement(directory.RefOnly ? "DirectoryRef" : "Directory");
                doc.WriteAttributeString("Id", directory.Id);
                if (!directory.RefOnly)
                    doc.WriteAttributeString("Name", directory.Name);
                WriteSubDirectories(doc, directory);
                doc.WriteEndElement();
            }
        }

        private void WriteDirectoryCustomizations(XmlWriter doc, WixTargetDirectory rootDirectory)
        {
            var customizations = TraverseDfs(rootDirectory, d => d.Subdirectories)
                .Where(d => d.Customization != null)
                .Select(d => d.Customization);

            _additionalComponentIdsByGroups = new Dictionary<string, HashSet<string>>();

            foreach (var c in customizations)
            {
                // <Property> with RegistrySearch - getting saved value from registry if exists
                doc.WriteStartElement("Property");
                doc.WriteAttributeString("Id", c.WixPublicPropertyName);
                doc.WriteAttributeString("Secure", "yes");

                doc.WriteStartElement("RegistrySearch");
                doc.WriteAttributeString("Id", c.WixPublicPropertyName);
                doc.WriteAttributeString("Root", c.RegistryRoot.ToString());
                doc.WriteAttributeString("Key", c.RegistryStorageKey);
                doc.WriteAttributeString("Name", c.WixPublicPropertyName);
                doc.WriteAttributeString("Type", "raw");
                doc.WriteEndElement();

                doc.WriteEndElement();

                // first <SetProperty> - setting default value if saved value in registry was not found
                // and no value was provided to public property explicitly (via command line)
                string setDefaultValueActionName = null;
                if (c.DefaultValue != null)
                {
                    doc.WriteStartElement("SetProperty");
                    doc.WriteAttributeString("Id", c.Parent.Id);
                    setDefaultValueActionName = GetTargetDirCustomizationActionUniqueName("Set" + c.Parent.Id);
                    doc.WriteAttributeString("Action", setDefaultValueActionName);
                    doc.WriteAttributeString("Before", "CostFinalize");
                    doc.WriteAttributeString("Sequence", "both");
                    doc.WriteAttributeString("Value", c.DefaultValue);
                    doc.WriteValue("NOT " + c.WixPublicPropertyName);
                    doc.WriteEndElement();
                }

                // second <SetProperty> - setting public property to result value if it wasn't provided explicitly
                doc.WriteStartElement("SetProperty");
                doc.WriteAttributeString("Id", c.WixPublicPropertyName);
                doc.WriteAttributeString("After", setDefaultValueActionName ?? "CostFinalize");
                doc.WriteAttributeString("Sequence", "execute");
                doc.WriteAttributeString("Value", "[" + c.Parent.Id + "]");
                doc.WriteValue("NOT " + c.WixPublicPropertyName);
                doc.WriteEndElement();

                // third <SetProperty> - setting result value to public property if it is specified explicitly
                doc.WriteStartElement("SetProperty");
                doc.WriteAttributeString("Id", c.Parent.Id);
                var readableName = String.Format("Set_{0}_to_{1}", c.Parent.Id, c.WixPublicPropertyName);
                var resToPublicActionName = GetTargetDirCustomizationActionUniqueName(readableName);
                doc.WriteAttributeString("Action", resToPublicActionName);
                doc.WriteAttributeString("Before", "CostFinalize");
                doc.WriteAttributeString("Sequence", "both");
                doc.WriteAttributeString("Value", "[" + c.WixPublicPropertyName + "]");
                doc.WriteValue(c.WixPublicPropertyName);
                doc.WriteEndElement();

                // <DirectoryRef Id="TARGETDIR"> with component for saving value in Registry
                doc.WriteStartElement("DirectoryRef");
                doc.WriteAttributeString("Id", "TARGETDIR");

                doc.WriteStartElement("Component");
                var componentName = c.WixPublicPropertyName + "_TargetDirCustomizationComponent";
                var componentId = GetTargetDirCustomizationComponentId(componentName);
                doc.WriteAttributeString("Id", componentId);
                doc.WriteAttributeString("Guid", GetTargetDirCustomizationComponentGuid(componentName).ToString("B").ToUpperInvariant());
                doc.WriteAttributeString("KeyPath", "yes");
                if (c.Parent.MultiInstance != null)
                    doc.WriteAttributeString("MultiInstance", c.Parent.MultiInstance);

                doc.WriteStartElement("RegistryValue");
                doc.WriteAttributeString("Id", GetTargetDirCustomizationRegistryId(c.WixPublicPropertyName + "_RegistryId"));
                doc.WriteAttributeString("Root", c.RegistryRoot.ToString());
                doc.WriteAttributeString("Key", c.RegistryStorageKey);
                doc.WriteAttributeString("Name", c.WixPublicPropertyName);
                doc.WriteAttributeString("Value", "[" + c.WixPublicPropertyName + "]");
                doc.WriteAttributeString("Type", "string");
                doc.WriteAttributeString("KeyPath", "no");
                doc.WriteEndElement(); // <RegistryValue ...>

                doc.WriteEndElement(); // <Component ...>

                doc.WriteEndElement(); // <DirectoryRef ...>

                HashSet<string> additionalComponentIds;
                if (!_additionalComponentIdsByGroups.TryGetValue(c.Parent.ComponentGroupRef, out additionalComponentIds))
                    _additionalComponentIdsByGroups[c.Parent.ComponentGroupRef] = additionalComponentIds = new HashSet<string>();
                additionalComponentIds.Add(componentId);
            }
        }

        private string GetTargetDirCustomizationActionUniqueName(string name)
        {
            var guid = _guidProvider.Get(SwixGuidType.TargetDirCustomizationActionName, name);
            return MakeUniqueId(guid, name, MaxIdLength);
        }

        private string GetTargetDirCustomizationRegistryId(string name)
        {
            var guid = _guidProvider.Get(SwixGuidType.TargetDirCustomizationRegistryId, name);
            return MakeUniqueId(guid, name, MaxIdLength);
        }

        private string GetTargetDirCustomizationComponentId(string name)
        {
            var guid = GetTargetDirCustomizationComponentGuid(name);
            return MakeUniqueId(guid, name, MaxIdLength);
        }

        private Guid GetTargetDirCustomizationComponentGuid(string name)
        {
            return _guidProvider.Get(SwixGuidType.TargetDirCustomizationComponent, name);
        }

        private string GetComponentFullTargetPath(WixComponent component)
        {
            if (component.TargetDir != null)
                throw new InvalidOperationException("This method should not be called before handling inline targetDirs");
            var dir = _directories[component.TargetDirRef];
            return string.Format("{0}\\{1}", dir.GetFullTargetPath(), component.FileName);
        }

        private string GetComponentId(WixComponent component)
        {
            if (component.Id != null) return component.Id;
            var guid = _guidProvider.Get(SwixGuidType.Component, GetComponentFullTargetPath(component));
            return component.Id = MakeUniqueId(guid, component.FileName, MaxIdLength);
        }

        private string GetRemoveOnUninstallComponentId(WixTargetDirectory removedDir)
        {
            var guid = GetRemoveOnUninstallComponentGuid(removedDir);
            return MakeUniqueId(guid, "RemoveOnUninstall_" + removedDir.Name, MaxIdLength);
        }

        private Guid GetRemoveOnUninstallComponentGuid(WixTargetDirectory removedDir)
        {
            return _guidProvider.Get(SwixGuidType.RemoveOnUninstallComponent, removedDir.GetFullTargetPath());
        }

        private string GetCreateOnInstallComponentId(WixTargetDirectory createdDir)
        {
            var guid = GetCreateOnInstallComponentGuid(createdDir);
            return MakeUniqueId(guid, "CreateOnInstall_" + createdDir.Name, MaxIdLength);
        }

        private Guid GetCreateOnInstallComponentGuid(WixTargetDirectory createdDir)
        {
            return _guidProvider.Get(SwixGuidType.CreateOnInstallComponent, createdDir.GetFullTargetPath());
        }

        private string MakeUniqueId(Guid guid, string filename, int maxLength)
        {
            var readableId = MakeReadableId(filename, maxLength - 33); // 32 for guid and 1 for separation underscore
            return String.Format("{0}_{1:N}", readableId, guid);
        }

        private static string MakeReadableId(string filename, int maxIdLength)
        {
            int substringLength = Math.Min(filename.Length, maxIdLength);

            // if filename has max acceptable length and first char is digit - we will insert
            // underscore, thus taking one character less from filename itself
            if (substringLength == maxIdLength && filename[0] >= '0' && filename[0] <= '9')
                substringLength--;

            var sb = new StringBuilder(filename, 0, substringLength, substringLength + 1);
            CleanseForUseAsId(sb);

            if (filename[0] >= '0' && filename[0] <= '9')
                sb.Insert(0, '_');

            return sb.ToString();
        }

        private static void CleanseForUseAsId(StringBuilder sb)
        {
            for (int i = 0; i < sb.Length; i++)
            {
                bool allowed = sb[i] >= 'a' && sb[i] <= 'z' ||
                               sb[i] >= 'A' && sb[i] <= 'Z' ||
                               sb[i] == '_' ||
                               sb[i] >= '0' && sb[i] <= '9' && i > 0 ||
                               sb[i] == '.' && i > 0;
                if (!allowed)
                    sb[i] = '_';
            }
        }

        private static IEnumerable<T> TraverseDfs<T>(T root, Func<T, IEnumerable<T>> childrenSelector)
        {
            foreach (var child in childrenSelector(root))
            {
                yield return child;
                foreach (var subtreeNode in TraverseDfs(child, childrenSelector))
                    yield return subtreeNode;
            }
        }
    }
}