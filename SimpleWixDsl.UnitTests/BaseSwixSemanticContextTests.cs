using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using SimpleWixDsl.Ahl;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.UnitTests
{
    [TestFixture]
    public class BaseSwixSemanticContextTests
    {
        private class SemContextStub : BaseSwixSemanticContext
        {
            public SemContextStub() 
                : base(new AttributeContext())
            {
            }

            public Func<string, IEnumerable<AhlAttribute>, ISemanticContext> ItemFunc;
            public Func<IEnumerable<AhlAttribute>, ISemanticContext> S1Func;
            public Func<string, IEnumerable<AhlAttribute>, ISemanticContext> M1Func;

            [ItemHandler]
            public ISemanticContext ItemHandler(string key, IEnumerable<AhlAttribute> attributes)
            {
                return ItemFunc(key, attributes);
            }

            [SectionHandler("s1")]
            public ISemanticContext S1(IEnumerable<AhlAttribute> attributes)
            {
                return S1Func(attributes);
            }

            [MetaHandler("m1")]
            public ISemanticContext M1(string key, IEnumerable<AhlAttribute> attributes)
            {
                return M1Func(key, attributes);
            }
        }

        [Test]
        public void ItemLine_RoutedCorrectlyWithSameAttributes()
        {
            var sut = new SemContextStub();
            var expectedAttrs = new List<AhlAttribute>();
            var expectedResult = MockRepository.GenerateStub<ISemanticContext>();
            bool called = false;
            sut.ItemFunc = (key, attrs) =>
                {
                    called = true;
                    Assert.AreEqual("mykey", key);
                    Assert.AreSame(expectedAttrs, attrs);
                    sut.ItemFunc = null;
                    return expectedResult;
                };
            var result = sut.PushLine(0, null, "mykey", expectedAttrs);
            Assert.IsTrue(called);
            Assert.AreSame(expectedResult, result);
        }

        [Test]
        public void SectionLine_RoutedCorrectlyWithSameAttributes()
        {
            var sut = new SemContextStub();
            var expectedAttrs = new List<AhlAttribute>();
            var expectedResult = MockRepository.GenerateStub<ISemanticContext>();
            bool called = false;
            sut.S1Func = attrs =>
                {
                    called = true;
                    Assert.AreSame(expectedAttrs, attrs);
                    sut.S1Func = null;
                    return expectedResult;
                };
            var result = sut.PushLine(0, ":s1", null, expectedAttrs);
            Assert.IsTrue(called);
            Assert.AreSame(expectedResult, result);
        }

        [Test]
        public void MetaLine_RoutedCorrectlyWithSameAttributes()
        {
            var sut = new SemContextStub();
            var expectedAttrs = new List<AhlAttribute>();
            var expectedResult = MockRepository.GenerateStub<ISemanticContext>();
            bool called = false;
            sut.M1Func = (key, attrs) =>
                {
                    called = true;
                    Assert.AreEqual("mykey", key);
                    Assert.AreSame(expectedAttrs, attrs);
                    sut.M1Func = null;
                    return expectedResult;
                };
            var result = sut.PushLine(0, "?m1", "mykey", expectedAttrs);
            Assert.IsTrue(called);
            Assert.AreSame(expectedResult, result);
        }

        [Test]
        public void OneLinerChild_RoutedCorrectly()
        {
            var mr = new MockRepository();
            var sut = new SemContextStub();
            var expectedAttrs = new List<AhlAttribute>();
            var sectionContext = mr.StrictMock<ISemanticContext>();
            sectionContext.Expect(sc => sc.FinishItem());
            var itemContext = mr.StrictMock<ISemanticContext>();
            itemContext.Expect(i => i.OnFinished += null).IgnoreArguments();
            
            sectionContext.Expect(sc => sc.PushLine(0, null, "mykey", expectedAttrs))
                .Return(itemContext);
            sut.S1Func = _ => sectionContext;
            
            mr.ReplayAll();
            var result = sut.PushLine(0, "!s1", "mykey", expectedAttrs);
            Assert.AreSame(itemContext, result);
            itemContext.GetEventRaiser(i => i.OnFinished += null).Raise(itemContext, EventArgs.Empty);
            mr.VerifyAll();
        }

        [Test]
        [ExpectedException(typeof(SwixSemanticException))]
        public void UnsupportedSectionThrowsException()
        {
            var sut = new SemContextStub();
            sut.PushLine(0, ":sss", null, Enumerable.Empty<AhlAttribute>());
        }

        [Test]
        [ExpectedException(typeof(SwixSemanticException))]
        public void SectionWithKey_ThrowsAnError()
        {
            var sut = new SemContextStub();
            sut.PushLine(0, ":s1", "a", Enumerable.Empty<AhlAttribute>());
        }
    }
}