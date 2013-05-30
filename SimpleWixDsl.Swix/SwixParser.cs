using System.IO;

namespace SimpleWixDsl.Swix
{
    public class SwixParser
    {
        public static SwixModel Parse(StreamReader sourceStream)
        {
            return new SwixParser(sourceStream).Run();
        }

        private readonly StreamReader _sourceStream;

        private SwixParser(StreamReader sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public SwixModel Run()
        {
            
        }
    }
}