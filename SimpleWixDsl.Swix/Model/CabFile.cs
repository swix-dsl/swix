namespace SimpleWixDsl.Swix
{
    public class CabFile
    {
        public static CabFile FromContext(string name, IAttributeContext attributeContext)
        {
            var result = new CabFile(name);

            var compressionLevel = attributeContext.GetInheritedAttribute("compressionLevel");
            result.CompressionLevel = compressionLevel ?? "none";

            var splitStr = attributeContext.GetInheritedAttribute("split") ?? "1";
            if (!int.TryParse(splitStr, out var split))
                throw new SwixItemParsingException($"Can't parse split number for cabFile '{name}'");
            if (split <= 0 || split >= 100)
                throw new SwixItemParsingException($"Split number must be positive integer less than 100 in the cabFile '{name}'");
            result.Split = split;

            return result;
        }

        public string Name { get; set; }

        public string CompressionLevel { get; set; }

        /// <summary>
        /// Gets or sets number of chunks cab should be split into. It is useful because cab-generation by light.exe
        /// is single-threaded only and splitting single cab to several chunks could significantly speed up the
        /// msi generation on multi-core system.
        /// </summary>
        public int Split { get; set; }

        private CabFile(string name)
        {
            Name = name;
        }
    }
}