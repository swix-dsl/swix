using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Rhino.Mocks;
using SimpleWixDsl.Ahl;
using Is = Rhino.Mocks.Constraints.Is;

namespace SimpleWixDsl.UnitTests
{
    [TestFixture]
    public class LexerTests
    {
        private MockRepository _mocks;
        private IParsingContext _parsingContext;
        private IDisposable _orderedModeToken;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            _orderedModeToken = _mocks.Ordered();
            _parsingContext = _mocks.StrictMock<IParsingContext>();
        }

        [TearDown]
        public void TearDown()
        {
            _mocks.VerifyAll();
        }

        [Test]
        public void EmptyFileSilentlySucceeds()
        {
            RunTest("  ");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void TabCharResultsInException()
        {
            RunFailingTest(" \t ");
        }

        [Test]
        public void SimpleSection()
        {
            ExpectLine(1, 0, ":section", null);
            RunTest(":section");
        }

        [Test]
        public void SimpleOneLetterKeyword()
        {
            ExpectLine(1, 0, ":a", null);
            RunTest(":a");
        }

        [Test]
        public void SimpleItem()
        {
            ExpectLine(1, 0, null, "a");
            RunTest("a");
        }

        [Test]
        public void SimpleQuotedItem()
        {
            ExpectLine(1, 0, null, "a");
            RunTest(@"""a""");
        }

        [Test]
        public void SimpleSectionWithAttribute()
        {
            ExpectLine(1, 0, ":section", null, new AhlAttribute("val", "string"));
            RunTest(":section :: val=string");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void SimpleSectionWithAttribute_AttributeValueContainsEqualsAndNotQuoted()
        {
            RunFailingTest(":section :: val==string");
        }

        [Test]
        public void SimpleSectionWithOneLetterAttribute()
        {
            ExpectLine(1, 0, ":section", null, new AhlAttribute("a", "1"));
            RunTest(":section :: a=1");
        }

        [Test]
        public void SimpleSectionWithSeveralAttributes()
        {
            ExpectLine(1, 0, ":section", null, new AhlAttribute("val", "string"), new AhlAttribute("val2", "string2"));
            RunTest(":section :: val=string, val2=string2");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void SimpleSectionWithIncompleteArgumentList_Exception()
        {
            RunFailingTest(":section :: val=string,");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void SimpleSectionWithIncompleteLine_Exception()
        {
            RunFailingTest(":section ::");
        }

        [Test]
        public void KeyWithoutKeywordLine()
        {
            ExpectLine(1, 0, null, "key");
            RunTest("key");
        }

        [Test]
        public void KeywordAndKeyTogether()
        {
            ExpectLine(1, 0, ":section", "key");
            RunTest(":section key");
        }

        [Test]
        public void AllClusesAtOnce()
        {
            ExpectLine(1, 0, ":section", "key", new AhlAttribute("attr1", "val1"), new AhlAttribute("attr2", "val2"));
            RunTest(":section   key::attr1=val1  ,attr2 = val2");
        }

        [Test]
        public void SectionWithItem()
        {
            ExpectLine(1, 0, ":section", null);
            ExpectLine(2, 2, null, "item");
            RunTest(":section\n  item");
        }

        [Test]
        public void SimpleEmptyLinesAndSomeSpaces()
        {
            ExpectLine(2, 0, ":section", null);
            ExpectLine(6, 2, null, "item");
            RunTest("  \n:section\n\n     \n   \n  item");
        }

        [Test]
        public void SectionWithSubsectionAndSomeAttributes()
        {
            ExpectLine(1, 0, ":section", null, new AhlAttribute("a", "1"));
            ExpectLine(2, 1, ":subsection", null, new AhlAttribute("b", "2"));
            RunTest(":section :: a=1\n :subsection :: b=2");
        }

        [Test]
        public void SectionWithMixOfQuotedAndUnquotedAttrsAndItems()
        {
            ExpectLine(2, 0, ":s", null, new AhlAttribute("a", "1,"), new AhlAttribute("b", "2"));
            ExpectLine(3, 2, "!ss", "ssx", new AhlAttribute("c", "3"), new AhlAttribute("d", "4"));
            ExpectLine(4, 2, null, "y", new AhlAttribute("e", "5"), new AhlAttribute("f", "6"), new AhlAttribute("g", "7"));
            RunTest(@"
:s ::a=""1,"",b  =2 
  !ss   ""ssx""::c=3,d=4
  y::e=5,f=""6"",g=7");
        }

        [Test]
        public void LineWithCommentSkipped()
        {
            RunTest("  // something");
        }

        [Test]
        public void SectionWithCloseCommentCorrectlyProcessed()
        {
            ExpectLine(1, 0, ":s", null);
            RunTest(":s// dfdsfd");
        }

        [Test]
        public void AttributesInCommentsAreNotParsed()
        {
            ExpectLine(1, 0, ":s", null, new AhlAttribute("a", "1"));
            RunTest(":s :: a=1//, b=2");
        }

        [Test]
        public void DoubleSlashInsideQuotesNotTreatedAsComment()
        {
            ExpectLine(1, 0, null, "//aaa");
            RunTest("\"//aaa\"");
        }

        [Test]
        public void ComplexCaseWithCommentsAndQuotes()
        {
            ExpectLine(1, 0, ":s", "aa//bb", new AhlAttribute("attr", "//x//y\"//z"));
            RunTest(":s \"aa//bb\"::attr=\"//x//y\"\"//z\"//,real=\"comment");
        }

        private void RunFailingTest(string input)
        {
            RunTest(input, expectEof: false);
        }

        private void RunTest(string input)
        {
            RunTest(input, expectEof: true);
        }

        private void RunTest(string input, bool expectEof)
        {
            if (expectEof)
                _parsingContext.Expect(pc => pc.PushEof());
            _orderedModeToken.Dispose();
            _mocks.ReplayAll();
            var stream = new StringReader(input);
            var sut = new AhlLexer(_parsingContext, stream);
            sut.Run();
        }

        private void ExpectLine(int lineNumber, int indent, string keyword, string key, params AhlAttribute[] ahlAttributes)
        {
            _parsingContext.Expect(c => c.PushLine(lineNumber, indent, keyword, key, ahlAttributes))
                           .IgnoreArguments()
                           .Constraints(
                               Is.Equal(lineNumber),
                               Is.Equal(indent),
                               Is.Equal(keyword),
                               Is.Equal(key),
                               Is.Matching<List<AhlAttribute>>(list =>
                                   {
                                       CollectionAssert.AreEqual(ahlAttributes, list, new AhlAttributeComparer());
                                       return true;
                                   }));
        }
    }
}