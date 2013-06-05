﻿using System;
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
        private readonly SwixModel _model;
        private readonly GuidProvider _guidProvider;
        private readonly Dictionary<string, int> _cabFilesIds = new Dictionary<string, int>();

        public WxsGenerator(SwixModel model, GuidProvider guidProvider)
        {
            _model = model;
            _guidProvider = guidProvider;
            int id = 1;
            foreach (var cabFile in model.CabFiles)
                _cabFilesIds[cabFile.Name] = id++;

            var nonUniqueDirectoryReadableIds = FindNonUniqueDirectoryReadableIds();
            if (nonUniqueDirectoryReadableIds.Count > 0)
            {
                var msg = string.Format("The following directories' IDs are not unique: {0}. You should rename some of them or specify IDs explicitly.",
                                        String.Join(", ", nonUniqueDirectoryReadableIds));
                throw new SwixSemanticException(msg);
            }
        }

        private HashSet<string> FindNonUniqueDirectoryReadableIds()
        {
            var result = new HashSet<string>();
            var usedIds = new HashSet<string>();
            foreach (var dir in TraverseDfs(_model.RootDirectory, dir => dir.Subdirectories))
            {
                var id = GetDirectoryReadableId(dir);
                bool isUnique = usedIds.Add(id);
                if (!isUnique)
                    result.Add(id);
            }
            return result;
        }

        private static string GetDirectoryReadableId(WixTargetDirectory dir)
        {
            return dir.Id ?? MakeReadableId(dir.Name);
        }

        public void WriteToStream(StreamWriter target)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\n";
            settings.NewLineHandling = NewLineHandling.Entitize;
            using (var doc = XmlWriter.Create(target, settings))
            {
                doc.WriteStartDocument();
                doc.WriteStartElement("Wix", "http://schemas.microsoft.com/wix/2006/wi");

                doc.WriteStartElement("Fragment");

                WriteSubDirectories(doc, _model.RootDirectory);
                WriteCabFiles(doc, _model.CabFiles);
                WriteComponents(doc, _model.Components);

                doc.WriteEndElement();

                doc.WriteEndElement();
                doc.WriteEndDocument();
                doc.Flush();
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
            if (!_cabFilesIds.ContainsKey(component.CabFileRef))
                throw new SwixSemanticException(String.Format("Component {0} references cabFile {1} which was not declared", component.SourcePath, component.CabFileRef));
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
                doc.WriteAttributeString("CompressionLevel", "mszip");
                doc.WriteEndElement();
            }
        }

        private void WriteSubDirectories(XmlWriter doc, WixTargetDirectory rootDirectory)
        {
            foreach (var directory in rootDirectory.Subdirectories)
            {
                doc.WriteStartElement("Directory");
                doc.WriteAttributeString("Id", GetDirectoryReadableId(directory));
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
            return MakeUniqueId(guid, filename);
        }

        private string MakeUniqueId(Guid guid, string filename)
        {
            var readableId = MakeReadableId(filename);
            return String.Format("{0}_{1:N}", readableId, guid);
        }

        private static string MakeReadableId(string filename)
        {
            const int maxFilenameSubstringLength = 100;
            int substringLength = Math.Min(filename.Length, maxFilenameSubstringLength);

            // if filename has max acceptable length and first char is digit - we will insert
            // underscore, thus taking one character less from filename itself
            if (substringLength == maxFilenameSubstringLength && filename[0] >= '0' && filename[0] <= '9')
                substringLength--;

            // <optional-underscore><filename-substring><underscore><guid> == 1 + substringLength + 1 + 32
            var sb = new StringBuilder(filename, 0, substringLength, substringLength + 34);
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