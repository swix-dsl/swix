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
        private const int MaxLengthOfComponentId = 72;
        private const int MaxLengthOfDirectoryId = 72;
        private readonly SwixModel _model;
        private readonly GuidProvider _guidProvider;
        private readonly Dictionary<string, int> _cabFilesIds = new Dictionary<string, int>();
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
                _cabFilesIds[cabFile.Name] = id++;
        }

        private void VerifyCabFileRefs()
        {
            foreach (var component in _model.Components)
            {
                if (!_cabFilesIds.ContainsKey(component.CabFileRef))
                    throw new SwixSemanticException(String.Format("Component {0} references cabFile {1} which was not declared", component.SourcePath, component.CabFileRef));
            }
        }

        private void VerifyDirectoryRefs()
        {
            foreach (var component in _model.Components)
            {
                if (_nonUniqueDirectoryReadableIds.Contains(component.TargetDirRef))
                {
                    var msg = string.Format("Component {0} references directory via implicit ID {1} which is noq unique", component.SourcePath, component.TargetDirRef);
                    throw new SwixSemanticException(msg);
                }
                if (!_directories.ContainsKey(component.TargetDirRef))
                {
                    var msg = string.Format("Component {0} references undeclared directory ID {1}", component.SourcePath, component.TargetDirRef);
                    throw new SwixSemanticException(msg);
                }
            }
        }

        private void AssignDirectoryIds()
        {
            foreach (var dir in TraverseDfs(_model.RootDirectory, dir => dir.Subdirectories))
            {
                var id = dir.Id ?? MakeReadableId(dir.Name, MaxLengthOfDirectoryId);
                if (_nonUniqueDirectoryReadableIds.Contains(id))
                    id = GetDirectoryUniqueId(dir);
                dir.Id = id;
                _directories[id] = dir;
            }
        }

        // modifies directory structure to include all directories specified inline and modifies components
        // to have direct targetDirRefs there instead of combination targetDirRef/targetDir
        private void HandleInlineTargetDirSpecifications()
        {
            foreach (var component in _model.Components)
            {
                if (String.IsNullOrEmpty(component.TargetDir)) continue;
                string[] path = component.TargetDir.Split('\\');

                var dir = _directories[component.TargetDirRef];
                foreach (var nextDirName in path)
                {
                    var nextDir = dir.Subdirectories.Find(subdir => subdir.Name == nextDirName);
                    if (nextDir == null)
                    {
                        nextDir = new WixTargetDirectory(nextDirName, dir);
                        nextDir.Id = GetDirectoryUniqueId(nextDir);
                        dir.Subdirectories.Add(nextDir);
                    }
                    dir = nextDir;
                }
                component.TargetDirRef = dir.Id;
                component.TargetDir = null;
            }
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

        private string GetDirectoryUniqueId(WixTargetDirectory dir)
        {
            var readableId = dir.Id ?? MakeReadableId(dir.Name, MaxLengthOfDirectoryId - 33);
            var guid = _guidProvider.Get(SwixGuidType.Directory, GetFullPath(dir));
            return String.Format("{0}_{1:N}", readableId, guid);
        }

        private string GetFullPath(WixTargetDirectory dir)
        {
            if (dir.Parent == null)
                return dir.Name;
            return String.Format("{0}\\{1}", GetFullPath(dir.Parent), dir.Name);
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
            var componentGuid = _guidProvider.Get(SwixGuidType.Component, component.SourcePath);
            doc.WriteAttributeString("Guid", componentGuid.ToString("B").ToUpperInvariant());

            doc.WriteStartElement("File");
            doc.WriteAttributeString("Id", id);
            doc.WriteAttributeString("KeyPath", "yes");
            doc.WriteAttributeString("Source", component.SourcePath);
            doc.WriteAttributeString("DiskId", _cabFilesIds[component.CabFileRef].ToString(CultureInfo.InvariantCulture));
            doc.WriteEndElement();

            doc.WriteEndElement();
        }

        private void WriteCabFiles(XmlWriter doc, IEnumerable<CabFile> cabFiles)
        {
            foreach (var cabFile in cabFiles)
            {
                doc.WriteStartElement("Media");
                doc.WriteAttributeString("Id", _cabFilesIds[cabFile.Name].ToString(CultureInfo.InvariantCulture));
                doc.WriteAttributeString("Cabinet", cabFile.Name + ".cab");
                doc.WriteAttributeString("EmbedCab", "yes");
                doc.WriteAttributeString("CompressionLevel", cabFile.CompressionLevel);
                doc.WriteEndElement();
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

        private string GetComponentId(WixComponent component)
        {
            if (component.Id != null) return component.Id;
            int idx = component.SourcePath.LastIndexOf('\\');
            string filename = component.SourcePath.Substring(idx + 1);
            var guid = _guidProvider.Get(SwixGuidType.Component, component.SourcePath);
            return MakeUniqueId(guid, filename, MaxLengthOfComponentId);
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