﻿using System.IO;
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

        [Test]
        public void BunchOfDirectories()
        {
            var model = Parse(":directories\n TARGETDIR\n  Test\n   Subtest::id=123\n  \"Another test\"");
            Assert.AreEqual(1, model.RootDirectory.Subdirectories.Count);
            var targetDir = model.RootDirectory.Subdirectories[0];
            Assert.AreEqual("TARGETDIR", targetDir.Name);
            Assert.AreEqual(2, targetDir.Subdirectories.Count);
            var test = targetDir.Subdirectories[0];
            var anotherTest = targetDir.Subdirectories[1];
            Assert.AreEqual("Test", test.Name);
            Assert.AreEqual("Another test", anotherTest.Name);
            Assert.AreEqual(1, test.Subdirectories.Count);
            Assert.AreEqual("Subtest", test.Subdirectories[0].Name);
            Assert.AreEqual("123", test.Subdirectories[0].Id);
            Assert.AreEqual(0, anotherTest.Subdirectories.Count);
        }

        public SwixModel Parse(string input)
        {
            var stream = new StringReader(input);
            return SwixParser.Parse(stream);
        }
    }
}