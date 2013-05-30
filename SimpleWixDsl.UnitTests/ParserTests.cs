using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using SimpleWixDsl.Swix;
using Is = Rhino.Mocks.Constraints.Is;

namespace SimpleWixDsl.UnitTests
{
    [TestFixture]
    public class ParserTests
    {
        private MockRepository _mocks;
        private ISemanticContext _semanticContext;
        private IDisposable _orderedModeToken;
        private ParsingContext _sut;
        private List<PushLineObject> _scheduledPushLines;

        [SetUp]
        public void Setup()
        {
            _mocks = new MockRepository();
            _orderedModeToken = _mocks.Ordered();
            _semanticContext = _mocks.StrictMock<ISemanticContext>();
            _scheduledPushLines = new List<PushLineObject>();
        }

        [TearDown]
        public void TearDown()
        {
            _mocks.VerifyAll();
        }

        [Test]
        public void EmptyFileNoExceptions()
        {
            StartTest();
        }

        [Test]
        public void SimpleSection()
        {
            AddPushLine(1, 0, ":s", null);
            ExpectFinish();
            StartTest();
        }

        [Test]
        public void SectionWithItem()
        {
            AddPushLine(1, 0, ":s", null);
            AddPushLine(2, 1, null, "item");
            ExpectFinish();
            ExpectFinish();
            StartTest();
        }

        [Test]
        public void SectionWithTwoItems()
        {
            AddPushLine(1, 0, ":s", null);
            AddPushLine(2, 1, null, "item");
            ExpectFinish();
            AddPushLine(3, 1, null, "item2");
            ExpectFinish();
            ExpectFinish();
            StartTest();
        }

        [Test]
        public void OneSectionWithItemAndAnotherWithout()
        {
            AddPushLine(1, 0, ":s", null);
            AddPushLine(2, 1, null, "item");
            ExpectFinish();
            ExpectFinish();
            AddPushLine(3, 0, ":s2", null);
            ExpectFinish();
            StartTest();
        }

        [Test]
        public void SeveralLevelsDropAtOnce()
        {
            AddPushLine(1, 0, ":s", null);
            AddPushLine(2, 1, null, "item");
            AddPushLine(3, 2, ":s2", null);
            ExpectFinish();
            ExpectFinish();
            ExpectFinish();
            StartTest();
        }

        [Test]
        [ExpectedException(typeof(IndentationException))]
        public void InconsistentIndentThrows()
        {
            AddPushLine(1, 0, ":s", null);
            AddPushLine(2, 4, null, "item");
            ExpectFinish();
            AddFailingPushLine(3, 2, ":s2", null);
            StartFailingTest();
        }

        [Test]
        public void ChildItemOnSecondItem()
        {
            AddPushLine(1, 1, ":s", null);
            ExpectFinish();
            AddPushLine(2, 1, null, "item");
            AddPushLine(3, 2, ":s2", null);
            ExpectFinish();
            ExpectFinish();
            StartTest();
        }

        private class PushLineObject
        {
            public PushLineObject(int line, int indent, string keyword, string key, params AhlAttribute[] ahlAttributes)
            {
                Line = line;
                Indent = indent;
                Keyword = keyword;
                Key = key;
                Attributes = ahlAttributes.ToList();
            }

            public int Line { get; set; }
            public int Indent { get; set; }
            public string Keyword { get; set; }
            public string Key { get; set; }
            public List<AhlAttribute> Attributes { get; private set; }
        }

        private void StartTest()
        {
            StartTest(true);
        }

        private void StartFailingTest()
        {
            StartTest(false);
        }

        private void StartTest(bool expectFinalFinish)
        {
            if (expectFinalFinish)
                _semanticContext.Expect(sc => sc.FinishItem());
            _orderedModeToken.Dispose();
            _mocks.ReplayAll();
            _sut = new ParsingContext(_semanticContext);
            foreach (var plo in _scheduledPushLines)
            {
                _sut.PushLine(plo.Line, plo.Indent, plo.Keyword, plo.Key, plo.Attributes);
            }
            _sut.PushEof();
        }

        private void AddPushLine(int line, int indent, string keyword, string key, params AhlAttribute[] ahlAttributes)
        {
            var plo = new PushLineObject(line, indent, keyword, key, ahlAttributes);
            _semanticContext.Expect(sc => sc.PushLine(line, keyword, key, ahlAttributes))
                            .IgnoreArguments()
                            .Constraints(
                                Is.Equal(line),
                                Is.Equal(keyword),
                                Is.Equal(key),
                                Is.Matching<List<AhlAttribute>>(list =>
                                    {
                                        CollectionAssert.AreEqual(ahlAttributes, list, new AhlAttributeComparer());
                                        return true;
                                    }))
                            .Return(_semanticContext);
            _scheduledPushLines.Add(plo);
        }

        private void AddFailingPushLine(int line, int indent, string keyword, string key, params AhlAttribute[] ahlAttributes)
        {
            _scheduledPushLines.Add(new PushLineObject(line, indent, keyword, key, ahlAttributes));
        }

        private void ExpectFinish()
        {
            _semanticContext.Expect(sc => sc.FinishItem());
        }
    }
}