using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleWixDsl.Swix
{
    /// <summary>
    /// Represents main entry point to the swix file processing
    /// </summary>
    public class SwixProcessor
    {
        public static string TempDir;

        public static SwixModel Transform(string swixFilename,
                                          SwixGuidMode guidMode,
                                          string targetFolderPath,
                                          IDictionary<string, string> variableDefinitions = null)
        {
            var folderPath = Path.GetDirectoryName(swixFilename) ?? Path.GetPathRoot(swixFilename);
            var baseName = Path.GetFileNameWithoutExtension(swixFilename);
            var guidProviderFileName = Path.Combine(folderPath, $"{baseName}.guid.info");
            if (File.Exists(guidProviderFileName))
                StripReadonlyIfSet(guidProviderFileName);

            var outputFile = Path.Combine(targetFolderPath, $"{baseName}.generated.wxs");
            if (File.Exists(outputFile))
                StripReadonlyIfSet(outputFile);

            TempDir = Path.Combine(targetFolderPath, "swixtemp");
            try
            {
                if (Directory.Exists(TempDir))
                {
                    Directory.Delete(TempDir, recursive: true);
                    Directory.CreateDirectory(TempDir);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Warning: Cannot clean temp directory {TempDir}: {e}");
            }

            GuidProvider guidProvider;
            if (guidMode != SwixGuidMode.AlwaysGenerateNew && File.Exists(guidProviderFileName))
            {
                using (var guidReader = new StreamReader(guidProviderFileName))
                    guidProvider = GuidProvider.CreateFromStream(guidReader, treatAbsentGuidAsError: guidMode == SwixGuidMode.TreatAbsentGuidAsError);
            }
            else
            {
                if (guidMode == SwixGuidMode.TreatAbsentGuidAsError)
                    throw new SwixSemanticException(0, $"TreatAbsentGuidAsError mode is active, but no {guidProviderFileName} file is found");
                guidProvider = new GuidProvider(treatAbsentGuidAsError: false);
            }

            SwixModel model;
            using (var sourceReader = new StreamReader(swixFilename))
            {
                model = SwixParser.Parse(sourceReader, guidProvider, variableDefinitions);
            }

            var wxsGenerator = new WxsGenerator(model, guidProvider);
            using (var outputStream = new StreamWriter(outputFile))
                wxsGenerator.WriteToStream(outputStream);

            if (guidMode == SwixGuidMode.UseExistingAndExtendStorage)
            {
                using (var guidOutputStream = new StreamWriter(guidProviderFileName))
                    guidProvider.SaveToStream(guidOutputStream, pruneUnused: false);
            }
            else if (guidMode == SwixGuidMode.UseExistingAndUpdateStorage)
            {
                using (var guidOutputStream = new StreamWriter(guidProviderFileName))
                    guidProvider.SaveToStream(guidOutputStream, pruneUnused: true);
            }

            return model;
        }

        private static void StripReadonlyIfSet(string filename)
        {
            var outputFileAttributes = File.GetAttributes(filename);
            if ((outputFileAttributes & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(filename, outputFileAttributes ^ FileAttributes.ReadOnly);
        }
    }
}
