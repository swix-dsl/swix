using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Rhino.Mocks;
using SimpleWixDsl.Swix;
using Attribute = SimpleWixDsl.Swix.Attribute;
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
            RunTest(" \t ");
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
            ExpectLine(1, 0, ":section", null, new Attribute("val", "string"));
            RunTest(":section :: val=string");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void SimpleSectionWithAttribute_AttributeValueContainsEqualsAndNotQuoted()
        {
            RunTest(":section :: val==string");
        }

        [Test]
        public void SimpleSectionWithOneLetterAttribute()
        {
            ExpectLine(1, 0, ":section", null, new Attribute("a", "1"));
            RunTest(":section :: a=1");
        }

        [Test]
        public void SimpleSectionWithSeveralAttributes()
        {
            ExpectLine(1, 0, ":section", null, new Attribute("val", "string"), new Attribute("val2", "string2"));
            RunTest(":section :: val=string, val2=string2");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void SimpleSectionWithIncompleteArgumentList_Exception()
        {
            RunTest(":section :: val=string,");
        }

        [Test]
        [ExpectedException(typeof (LexerException))]
        public void SimpleSectionWithIncompleteLine_Exception()
        {
            RunTest(":section ::");
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
            ExpectLine(1, 0, ":section", "key", new Attribute("attr1", "val1"), new Attribute("attr2", "val2"));
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
            ExpectLine(1, 0, ":section", null, new Attribute("a", "1"));
            ExpectLine(2, 1, ":subsection", null, new Attribute("b", "2"));
            RunTest(":section :: a=1\n :subsection :: b=2");
        }

        [Test]
        public void SectionWithMixOfQuotedAndUnquotedAttrsAndItems()
        {
            ExpectLine(2, 0, ":s", null, new Attribute("a", "1,"), new Attribute("b", "2"));
            ExpectLine(3, 2, "!ss", "ssx", new Attribute("c", "3"), new Attribute("d", "4"));
            ExpectLine(4, 2, null, "y", new Attribute("e", "5"), new Attribute("f", "6"), new Attribute("g", "7"));
            RunTest(@"
:s ::a=""1,"",b  =2 
  !ss   ""ssx""::c=3,d=4
  y::e=5,f=""6"",g=7");
        }

        private void RunTest(string input)
        {
            _orderedModeToken.Dispose();
            _mocks.ReplayAll();
            var stream = new StringReader(input);
            var sut = new AhlLexer(_parsingContext, stream);
            sut.Run();
        }

        private void ExpectLine(int lineNumber, int indent, string keyword, string key, params Attribute[] attributes)
        {
            _parsingContext.Expect(c => c.PushLine(lineNumber, indent, keyword, key, attributes))
                           .IgnoreArguments()
                           .Constraints(
                               Is.Equal(lineNumber),
                               Is.Equal(indent),
                               Is.Equal(keyword),
                               Is.Equal(key),
                               Is.Matching<List<Attribute>>(list =>
                                   {
                                       CollectionAssert.AreEqual(attributes, list, new AttributeComparer());
                                       return true;
                                   }));
        }

        private class AttributeComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var a = x as Attribute;
                var b = y as Attribute;
                if (a == null && b == null) return 0;
                if (a == null) return -1;
                if (b == null) return 1;

                if (a.Key == b.Key && a.Value == b.Value) return 0;

                int keyComparison = String.CompareOrdinal(a.Key, b.Key);
                return keyComparison != 0 ? keyComparison : String.CompareOrdinal(a.Value, b.Value);
            }
        }
    }
}