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

        private const int MaxLengthOfComponentId = 72;
        private const int MaxLengthOfDirectoryId = 72;
        private const int MaxLengthOfShortcutId = 72;
        private readonly SwixModel _model;
        private readonly GuidProvider _guidProvider;
        private readonly Dictionary<string, CabFileCounter> _cabFileCounters = new Dictionary<string, CabFileCounter>();
        private readonly Dictionary<string, WixTargetDirectory> _directories = new Dictionary<string, WixTargetDirectory>();
        private HashSet<string> _nonUniqueDirectoryReadableIds;

        public WxsGenerator(SwixModel model, GuidProvider guidProvider)
        {
            _model = model;
            _guidProvider = guidProvider;

            AssignCabFileIds();
            FindNonUniqueDirectoryReadableIds();
            AssignDirectoryIds();
            VerifyDirectoryRefs();
            VerifyCabFileRefs();
            HandleInlineTargetDirSpecifications();
        }

        private void AssignCabFileIds()
        {
            int id = 1;
            foreach (var cabFile in _model.CabFiles)
            {
                _cabFileCounters[cabFile.Name] = new CabFileCounter(id, cabFile.Split);
                id += cabFile.Split;
            }
        }

        private void VerifyCabFileRefs()
        {
            foreach (var component in _model.Components)
            {
                if (!_cabFileCounters.ContainsKey(component.CabFileRef))
                    throw new SwixSemanticException(String.Format("Component {0} references cabFile {1} which was not declared", component.SourcePath, component.CabFileRef));
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
                throw new SwixSemanticException(msg);
            }
            if (!_directories.ContainsKey(targetDirRef))
            {
                var msg = string.Format("{0} {1} references undeclared directory ID {2}",
                                        referencingEntityType,
                                        referencingEntityName,
                                        targetDirRef);
                throw new SwixSemanticException(msg);
            }
        }

        private void AssignDirectoryIds()
        {
            foreach (var dir in TraverseDfs(_model.RootDirectory, dir => dir.Subdirectories))
            {
                var id = dir.Id ?? MakeReadableId(dir.Name, MaxLengthOfDirectoryId);
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
                var id = dir.Id ?? MakeReadableId(dir.Name, MaxLengthOfDirectoryId);
                bool isUnique = usedIds.Add(id);
                if (!isUnique)
                    _nonUniqueDirectoryReadableIds.Add(id);
            }
        }

        private void AssignDirectoryUniqueId(WixTargetDirectory dir)
        {
            var readableId = dir.Id ?? MakeReadableId(dir.Name, MaxLengthOfDirectoryId - 33);
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

                WriteComponentGroups(doc);
                WriteSubDirectories(doc, _model.RootDirectory);
                WriteCabFiles(doc, _model.CabFiles);
                WriteComponents(doc, _model.Components);

                doc.WriteEndElement();

                doc.WriteEndElement();
                doc.WriteEndDocument();
                doc.Flush();
            }
        }

        private void WriteComponentGroups(XmlWriter doc)
        {
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

                doc.WriteEndElement();
            }
        }

        private void WriteComponents(XmlWriter doc, IEnumerable<WixComponent> components)
        {
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

        private void WriteComponent(XmlWriter doc, WixComponent component)
        {
            doc.WriteStartElement("Component");
            var id = GetComponentId(component);
            doc.WriteAttributeString("Id", id);
            if (component.MultiInstance != null)
                doc.WriteAttributeString("MultiInstance", component.MultiInstance);
            var componentGuid = _guidProvider.Get(SwixGuidType.Component, GetComponentFullTargetPath(component));
            doc.WriteAttributeString("Guid", componentGuid.ToString("B").ToUpperInvariant());

            doc.WriteStartElement("File");
            doc.WriteAttributeString("Id", id);
            doc.WriteAttributeString("KeyPath", "yes");
            doc.WriteAttributeString("Source", component.SourcePath);
            doc.WriteAttributeString("Name", component.FileName);
            doc.WriteAttributeString("DiskId", _cabFileCounters[component.CabFileRef].GetNextId().ToString(CultureInfo.InvariantCulture));

            WriteComponentShortcuts(doc, component);

            doc.WriteEndElement();

            doc.WriteEndElement();
        }

        private void WriteComponentShortcuts(XmlWriter doc, WixComponent component)
        {
            foreach (var shortcut in component.Shortcuts)
            {
                doc.WriteStartElement("Shortcut");
                var fullPath = string.Format("{0}\\{1}", _directories[shortcut.TargetDirRef].GetFullTargetPath(), shortcut.Name);
                var guid = _guidProvider.Get(SwixGuidType.Shortcut, fullPath);
                var id = MakeUniqueId(guid, shortcut.Name, MaxLengthOfShortcutId);
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
                doc.WriteStartElement("Directory");
                doc.WriteAttributeString("Id", directory.Id);
                doc.WriteAttributeString("Name", directory.Name);
                WriteSubDirectories(doc, directory);
                doc.WriteEndElement();
            }
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
            return component.Id = MakeUniqueId(guid, component.FileName, MaxLengthOfComponentId);
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
                               sb[i] >= '0' && sb[i] <= '9' && i > 0 ||
                               sb[i] == '_' || sb[i] == '.';
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