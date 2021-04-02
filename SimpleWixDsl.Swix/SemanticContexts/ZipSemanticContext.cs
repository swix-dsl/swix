using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ZipSemanticContext : ComponentsSection
    {
        public ZipSemanticContext(int line, string archivePath, IAttributeContext context, List<WixComponent> components)
            : base(line, context, components)
        {
            var originalFrom = context.GetInheritedAttribute("from");
            if (originalFrom != null)
                archivePath = Path.Combine(originalFrom, archivePath);

            if (!File.Exists(archivePath))
                throw new SwixSemanticException(line, $"File {archivePath} not found");

            var tmp = SwixProcessor.TempDir;
            var g = context.GuidProvider.Get(SwixGuidType.ZipArchive, archivePath.ToLowerInvariant()).ToString("N");
            var unpackedPath = Path.GetFullPath(Path.Combine(tmp, g));
            try
            {
                ZipFile.ExtractToDirectory(archivePath, unpackedPath);
            }
            catch (Exception e)
            {
                throw new SwixSemanticException(line, $"Error while unzipping {archivePath} to {unpackedPath}: {e}");
            }

            context.SetAttributes(new[]
            {
                new AhlAttribute("fromBase", unpackedPath),
                new AhlAttribute("from", null)
            });

            int rootSubstringLength = unpackedPath.Length;

            var files = Directory.GetFiles(unpackedPath);
            foreach (string path in files)
            {
                var component = WixComponent.FromContext(path, context);
                if (path[rootSubstringLength] == '\\')
                    rootSubstringLength++;
                var relativeDir = Path.GetDirectoryName(path.Substring(rootSubstringLength));
                if (relativeDir != null)
                {
                    component.TargetDir = component.TargetDir == null
                        ? relativeDir
                        : Path.Combine(component.TargetDir, relativeDir);
                }

                GatheredComponents.Add(component);
            }
        }
    }
}
