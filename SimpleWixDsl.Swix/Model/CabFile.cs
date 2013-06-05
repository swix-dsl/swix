namespace SimpleWixDsl.Swix
{
    public class CabFile
    {
        public static CabFile FromContext(string name, IAttributeContext attributeContext)
        {
            var result = new CabFile(name);

            var compressionLevel = attributeContext.GetInheritedAttribute("compressionLevel");
            result.CompressionLevel = compressionLevel ?? "none";

            return result;
        }

        public string Name { get; set; }

        public string CompressionLevel { get; set; }

        private CabFile(string name)
        {
            Name = name;
        }
    }
}