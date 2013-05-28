using NUnit.Framework;
using SimpleWixDsl.Swix;
using SimpleWixDsl.Swix.Parsing;

namespace SimpleWixDsl.UnitTests
{
    [TestFixture]
    public class LexerTests
    {
        [Test]
        public void Ctor_DoesntThrow()
        {
            new SwixLexer(null);
        }

        [Test]
        public void NAME()
        {
            
        }
    }
}
