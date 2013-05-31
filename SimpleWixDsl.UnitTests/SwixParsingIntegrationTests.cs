using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.UnitTests
{
    [TestFixture]
    public class SwixParsingIntegrationTests
    {
        [Test]
        public void BunchOfCabFiles()
        {
            var model = Parse(":cabFiles\n  cab1\n  cab2 \n  cab3");
            CollectionAssert.AreEqual(new[] {"cab1", "cab2", "cab3"}, model.CabFiles.Select(cf => cf.Name));
        }

        public SwixModel Parse(string input)
        {
            var stream = new StringReader(input);
            return SwixParser.Parse(stream);
        }
    }
}