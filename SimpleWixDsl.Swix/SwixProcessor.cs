using System.IO;

namespace SimpleWixDsl.Swix
{
    /// <summary>
    /// Represents main entry point to the swix file processing
    /// </summary>
    public class SwixProcessor
    {
        public static void Transform(string swixFilename)
        {
            var folderPath = Path.GetDirectoryName(swixFilename) ?? Path.GetPathRoot(swixFilename);
            var baseName = Path.GetFileNameWithoutExtension(swixFilename);
            var guidProviderFileName = Path.Combine(folderPath, baseName + ".guid.info");
            var outputFile = Path.Combine(folderPath, baseName + ".wxs");

            SwixModel model;
            using (var sourceReader = new StreamReader(swixFilename))
            {
                try
                {
                    model = SwixParser.Parse(sourceReader);
                }
                catch (SwixSemanticException e)
                {
                    throw new SwixSemanticException(string.Format("{0} {1}", swixFilename, e.Message));
                }
            }

            GuidProvider guidProvider;
            if (File.Exists(guidProviderFileName))
            {
                using (var guidReader = new StreamReader(guidProviderFileName))
                    guidProvider = GuidProvider.CreateFromStream(guidReader);
            }
            else
            {
                guidProvider = new GuidProvider();
            }

            var wxsGenerator = new WxsGenerator(model, guidProvider);
            using (var outputStream = new StreamWriter(outputFile))
                wxsGenerator.WriteToStream(outputStream);
            using (var guidOutputStream = new StreamWriter(guidProviderFileName))
                guidProvider.SaveToStream(guidOutputStream);
        }
    }
}