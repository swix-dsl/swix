using System.IO;

namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public static class Validators
    {
        public static bool FileSystemPath(string value)
        {
            var cs = Path.GetInvalidPathChars();
            return value.IndexOfAny(cs) != -1;
        }
    }
}