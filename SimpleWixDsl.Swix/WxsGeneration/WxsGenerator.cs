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
        private readonly Dictionary<string, CabFileCounter> _cabFileCounters = new Dictionary<string, CabFileCounter>();
        private readonly Dictionary<string, WixTargetDirectory> _directories = new Dictionary<string, WixTargetDirectory>();
        private readonly GuidProvider _guidProvider;

        private readonly SwixModel _model;
        private Dictionary<string, HashSet<string>> _additionalComponentIdsByGroups;
        private bool? _forModule;
        private HashSet<string> _nonUniqueDirectoryReadableIds;

        public WxsGenerator(SwixModel model, GuidProvider guidProvider)
        {
            _model = model;
            _guidProvider = guidProvider;

            DetectIfModule();
            VerifyDirectories();
            AssignCabFileIds();
            FindNonUniqueDirectoryReadableIds();
            AssignDirectoryIds();
            VerifyDirectoryRefs();
            VerifyCabFileRefs();
            HandleInlineTargetDirSpecifications();
        }

        private int MaxIdLength
        {
            get
            {
                if (_forModule == null)
                    throw new InvalidOperationException("You can't call this property right now");

                // when merging MSM module into MSI, its IDs are prepended with MSM guid, and their total length
                // should be less than or equal to 72. Prepended guid is 36 symbols, plus there's one dot added
                // before it, so in total it is 37 symbols added
                return _forModule == true ? 72 - 37 : 72;
            }
        }

        private void VerifyDirectories()
        {
            foreach (var subDir in _model.RootDirectory.Subdirectories)
                VerifySubdirectoriesDontHaveRefOnlyAttributeSet(subDir);
            VerifyDirectoriesMarkedAsCreateOnInstallOrRemoveOnUninstallOrCustomizedHaveComponentGroupRefSet(_model.RootDirectory);
        }

        private void VerifySubdirectoriesDontHaveRefOnlyAttributeSet(WixTargetDirectory dir)
        {
            foreach (var subDir in dir.Subdirectories)
            {
                if (subDir.RefOnly)
                    throw new SwixSemanticException(0, $"Directory {subDir.GetFullTargetPath()} is marked as refOnly and has parent. refOnly dirs can be only top-level");
                VerifySubdirectoriesDontHaveRefOnlyAttributeSet(subDir);
            }
        }

        private void VerifyDirectoriesMarkedAsCreateOnInstallOrRemoveOnUninstallOrCustomizedHaveComponentGroupRefSet(WixTargetDirectory root)
        {
            var invalidDirectories = TraverseDfs(root, d => d.Subdirectories)
                .Where(d => (d.RemoveOnUninstall || d.CreateOnInstall || d.Customization != null) && string.IsNullOrWhiteSpace(d.ComponentGroupRef))
                .Select(d => d.GetFullTargetPath())
                .ToArray();
            if (invalidDirectories.Any())
            {
                var dirList = string.Join(", ", invalidDirectories);
                throw new SwixSemanticException(0, $"Directories {dirList} are customized or marked as createOnInstall/removeOnUninstall but don't have valid ComponentGroupRef assigned");
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
                if (!_cabFileCounters.ContainsKey(component.CabFileRef))
                    throw new SwixSemanticException(0, $"Component {component.SourcePath} references cabFile {component.CabFileRef} which was not declared");
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
                throw new SwixSemanticException(0, $"{referencingEntityType} {referencingEntityName} references directory via implicit ID {targetDirRef} which is not unique");

            if (!_directories.ContainsKey(targetDirRef))
                throw new SwixSemanticException(0, $"{referencingEntityType} {referencingEntityName} references undeclared directory ID {targetDirRef}");
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
            var path = targetDir.Split('\\');
            var dir = _directories[targetDirRef];
            foreach (var nextDirName in path)
            {
                var nextDir = dir.Subdirectories.Find(subDir => subDir.Name == nextDirName);
                if (nextDir == null)
                {
                    nextDir = new WixTargetDirectory(nextDirName, dir);
                    AssignDirectoryUniqueId(nextDir);
                    dir.Subdirectories.Add(nextDir);
                }

                dir = nextDir;
            }

            var resultingTargetDirRef = dir.Id;
            return resultingTargetDirRef;
        }

        private void FindNonUniqueDirectoryReadableIds()
        {
            _nonUniqueDirectoryReadableIds = new HashSet<string>();
            var usedIds = new HashSet<string>();
            foreach (var dir in TraverseDfs(_model.RootDirectory, dir => dir.Subdirectories))
            {
                var id = dir.Id ?? MakeReadableId(dir.Name, MaxIdLength);
                var isUnique = usedIds.Add(id);
                if (!isUnique)
                    _nonUniqueDirectoryReadableIds.Add(id);
            }
        }

        private void AssignDirectoryUniqueId(WixTargetDirectory dir)
        {
            var readableId = dir.Id ?? MakeReadableId(dir.Name, MaxIdLength - 33);
            var guid = _guidProvider.Get(SwixGuidType.Directory, dir.GetFullTargetPath());
            var finalId = $"{readableId}_{guid:N}";
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

        private void DetectIfModule()
        {
            _forModule = _model.Components.Any(c => c.ModuleRef != null);
        }

        private void WriteComponentGroups(XmlWriter doc)
        {
            if (_additionalComponentIdsByGroups == null)
                throw new InvalidOperationException("This method can't be executed until _additionalComponentIdsByGroups is filled");

            var groupedComponents = _model.Components.ToLookup(c => c.ComponentGroupRef);
            foreach (var group in groupedComponents)
            {
                var componentGroupRef = group.Key;
                WriteComponentGroupRef(doc, componentGroupRef, group);
            }

            var groupsWithoutFileComponents = TraverseDfs(_model.RootDirectory, d => d.Subdirectories)
                .Where(d => (d.RemoveOnUninstall || d.CreateOnInstall) && !groupedComponents.Contains(d.ComponentGroupRef))
                .Select(d => d.ComponentGroupRef)
                .Distinct();
            foreach (var compGroup in groupsWithoutFileComponents)
                WriteComponentGroupRef(doc, compGroup, Enumerable.Empty<WixComponent>());
        }

        private void WriteComponentGroupRef(XmlWriter doc, string componentGroupRef, IEnumerable<WixComponent> fileComponents)
        {
            doc.WriteStartElement("ComponentGroup");
            doc.WriteAttributeString("Id", componentGroupRef);

            foreach (var component in fileComponents)
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

            if (_additionalComponentIdsByGroups.TryGetValue(componentGroupRef, out var additionalComponentIds))
                foreach (var additionalId in additionalComponentIds)
                {
                    doc.WriteStartElement("ComponentRef");
                    doc.WriteAttributeString("Id", additionalId);
                    doc.WriteEndElement();
                }

            doc.WriteEndElement();
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
                var fullPath = $"{_directories[shortcut.TargetDirRef].GetFullTargetPath()}\\{shortcut.Name}";
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

                if (service.Account != null)
                    doc.WriteAttributeString("Account", service.Account);

                if (service.Password != null)
                    doc.WriteAttributeString("Password", service.Password);

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
                    var id = $"{counter.StartId + i}";
                    var filename = $"{cabFile.Name}_{counter.StartId + i:D2}.cab";
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
                var readableName = $"Set_{c.Parent.Id}_to_{c.WixPublicPropertyName}";
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

                if (!_additionalComponentIdsByGroups.TryGetValue(c.Parent.ComponentGroupRef, out var additionalComponentIds))
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
            return $"{dir.GetFullTargetPath()}\\{component.FileName}";
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
            return $"{readableId}_{guid:N}";
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

            public int StartId { get; }

            public int GetNextId() => StartId + _current++ % _splitNumber;
        }
    }
}
