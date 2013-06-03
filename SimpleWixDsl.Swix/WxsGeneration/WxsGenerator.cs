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
        private readonly SwixModel _model;
        private readonly GuidProvider _guidProvider;
        private readonly Dictionary<string, int> _cabFilesIds = new Dictionary<string, int>();

        public WxsGenerator(SwixModel model, GuidProvider guidProvider)
        {
            _model = model;
            _guidProvider = guidProvider;
            int id = 1;
            foreach (var cabFile in model.CabFiles)
            {
                _cabFilesIds[cabFile.Name] = id++;
            }
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
            doc.WriteAttributeString("Guid", componentGuid.ToString("B"));

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
                doc.WriteAttributeString("Cabinet", cabFile.Name);
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
                var guid = _guidProvider.Get(SwixGuidType.Directory, directory.Name);
                doc.WriteAttributeString("Id", directory.Id ?? MakeUniqueId(guid, directory.Name));
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
            if (filename[0] >= '0' && filename[0] <= '9')
                filename = "_" + filename;
            if (filename.Length > 100)
                filename = filename.Substring(0, 100);
            var sb = new StringBuilder(filename, filename.Length + 33);
            for (int i = 0; i < sb.Length; i++)
            {
                bool allowed = sb[i] >= 'a' && sb[i] <= 'z' ||
                               sb[i] >= 'A' && sb[i] <= 'Z' ||
                               sb[i] >= '0' && sb[i] <= '9' && i > 0 ||
                               sb[i] == '_';
                if (!allowed)
                    sb[i] = '_';
            }
            sb.AppendFormat("_{0:N}", guid);
            return sb.ToString();
        }
    }
}