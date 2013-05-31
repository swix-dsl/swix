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

            public Func<string, IAttributeContext, ISemanticContext> ItemFunc;
            public Func<IAttributeContext, ISemanticContext> S1Func;
            public Func<string, IAttributeContext, ISemanticContext> M1Func;
            public Func<IAttributeContext> AttributeContextFactory = () => new AttributeContext();
                
            [ItemHandler]
            public ISemanticContext ItemHandler(string key, IAttributeContext itemContext)
            {
                return ItemFunc(key, itemContext);
            }

            [SectionHandler("s1")]
            public ISemanticContext S1(IAttributeContext childContext)
            {
                return S1Func(childContext);
            }

            [MetaHandler("m1")]
            public ISemanticContext M1(string key, IAttributeContext metaContext)
            {
                return M1Func(key, metaContext);
            }

            protected override IAttributeContext CreateNewAttributeContext()
            {
                return AttributeContextFactory();
            }
        }

        [Test]
        public void ItemLine_RoutedCorrectlyWithSameAttributes()
        {
            var sut = new SemContextStub();
            var expectedChildContext = MockRepository.GenerateStub<IAttributeContext>();
            sut.AttributeContextFactory = () => expectedChildContext;

            var expectedResult = MockRepository.GenerateStub<ISemanticContext>();
            bool called = false;
            sut.ItemFunc = (key, context) =>
                {
                    called = true;
                    Assert.AreEqual("mykey", key);
                    Assert.AreSame(expectedChildContext, context);
                    sut.ItemFunc = null;
                    return expectedResult;
                };
            var result = sut.PushLine(0, null, "mykey", new List<AhlAttribute>());
            Assert.IsTrue(called);
            Assert.AreSame(expectedResult, result);
        }

        [Test]
        public void SectionLine_RoutedCorrectlyWithSameAttributes()
        {
            var sut = new SemContextStub();
            var attributeContext = MockRepository.GenerateStub<IAttributeContext>();
            sut.AttributeContextFactory = () => attributeContext;
            var expectedResult = MockRepository.GenerateStub<ISemanticContext>();
            
            bool called = false;
            sut.S1Func = context =>
                {
                    called = true;
                    Assert.AreSame(attributeContext, context);
                    sut.S1Func = null;
                    return expectedResult;
                };
            var result = sut.PushLine(0, ":s1", null, new List<AhlAttribute>());
            Assert.IsTrue(called);
            Assert.AreSame(expectedResult, result);
        }

        [Test]
        public void MetaLine_RoutedCorrectlyWithSameAttributes()
        {
            var sut = new SemContextStub();
            var expectedChildContext = MockRepository.GenerateStub<IAttributeContext>();
            sut.AttributeContextFactory = () => expectedChildContext;
            var expectedResult = MockRepository.GenerateStub<ISemanticContext>();
            bool called = false;
            sut.M1Func = (key, context) =>
                {
                    called = true;
                    Assert.AreEqual("mykey", key);
                    Assert.AreSame(expectedChildContext, context);
                    sut.M1Func = null;
                    return expectedResult;
                };
            var result = sut.PushLine(0, "?m1", "mykey", new AhlAttribute[0]);
            Assert.IsTrue(called);
            Assert.AreSame(expectedResult, result);
        }

        [Test]
        public void OneLinerChild_RoutedCorrectly()
        {
            var mr = new MockRepository();
            var sut = new SemContextStub();
            var expectedChildContext = MockRepository.GenerateStub<IAttributeContext>();
            sut.AttributeContextFactory = () => expectedChildContext;
            var sectionContext = mr.StrictMock<ISemanticContext>();
            sectionContext.Expect(sc => sc.FinishItem());
            var itemContext = mr.StrictMock<ISemanticContext>();
            itemContext.Expect(i => i.OnFinished += null).IgnoreArguments();
            
            sectionContext.Expect(sc => sc.PushLine(0, null, "mykey", new AhlAttribute[0]))
                .Return(itemContext);
            sut.S1Func = _ => sectionContext;
            
            mr.ReplayAll();
            var result = sut.PushLine(0, "!s1", "mykey", new AhlAttribute[0]);
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