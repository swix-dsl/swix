using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using SimpleWixDsl.Ahl;
using SimpleWixDsl.Swix;
using Is = Rhino.Mocks.Constraints.Is;

namespace SimpleWixDsl.UnitTests
{
    [TestFixture]
    public class BaseSwixSemanticContextTests
    {
        private class SemContextStub : BaseSwixSemanticContext
        {
            public SemContextStub()
                : base(0, new AttributeContext(new Dictionary<string, string>()))
            {
                AttributeContextFactory = () => new AttributeContext(CurrentAttributeContext);
            }

            public Func<string, IAttributeContext, ISemanticContext> ItemFunc;
            public Func<IAttributeContext, ISemanticContext> S1Func;
            public Func<string, IAttributeContext, ISemanticContext> M1Func;
            public Func<IAttributeContext> AttributeContextFactory;

            public IDictionary<string, string> Variables
            {
                get { return CurrentAttributeContext.SwixVariableDefinitions; }
            }

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

            sectionContext.Expect(sc => sc.PushLine(0, null, null, null))
                          .Constraints(Is.Equal(0), Is.Same(null), Is.Equal("mykey"), Is.Anything())
                          .Return(itemContext);
            sut.S1Func = _ => sectionContext;

            mr.ReplayAll();
            var result = sut.PushLine(0, "!s1", "mykey", new AhlAttribute[0]);
            Assert.AreSame(itemContext, result);
            itemContext.GetEventRaiser(i => i.OnFinished += null).Raise(itemContext, EventArgs.Empty);
            mr.VerifyAll();
        }

        [Test]
        public void MetaDefaults_SubChildrenGetCorrectContextAndRoutedToSameContext()
        {
            var sut = new SemContextStub();

            bool itemFuncCalled = false;
            sut.ItemFunc = (s, context) =>
                {
                    itemFuncCalled = true;
                    Assert.AreEqual("v", context.GetInheritedAttribute("a"));
                    return MockRepository.GenerateStub<ISemanticContext>();
                };

            var underDefaults = sut.PushLine(0, "?set", null, new[] {new AhlAttribute("a", "v"),});
            Assert.IsFalse(itemFuncCalled);
            underDefaults.PushLine(1, null, "item", new AhlAttribute[0]);
            Assert.IsTrue(itemFuncCalled);
            underDefaults.FinishItem();

            bool itemFuncSecondCalled = false;
            sut.ItemFunc = (s, context) =>
                {
                    itemFuncSecondCalled = true;
                    Assert.IsNull(context.GetInheritedAttribute("a"));
                    return null;
                };
            sut.PushLine(2, null, "item2", new AhlAttribute[0]);
            Assert.IsTrue(itemFuncSecondCalled);
        }

        [Test]
        [ExpectedException(typeof (SwixSemanticException))]
        public void UnsupportedSectionThrowsException()
        {
            var sut = new SemContextStub();
            sut.PushLine(0, ":sss", null, Enumerable.Empty<AhlAttribute>());
        }

        [Test]
        [ExpectedException(typeof (SwixSemanticException))]
        public void SectionWithKey_ThrowsAnError()
        {
            var sut = new SemContextStub();
            sut.PushLine(0, ":s1", "a", Enumerable.Empty<AhlAttribute>());
        }

        [Test]
        public void VariableExpansion_ItemKeysIsVarReferenceEntirely_ChildContextReceivesValueExpanded()
        {
            CheckVariableExpansionInKey("$(swix.var.My)", "var-value", "var-value");
        }

        [Test]
        public void VariableExpansion_ItemKeyHasVarReferenceAndSomethingElse_ChildContextReceivesValueExpanded()
        {
            CheckVariableExpansionInKey("$(swix.var.My)+something", "var-value", "var-value+something");
        }

        [Test]
        public void VariableExpansion_VarValueIsSimilarToAnotherVarReference_ExpansionHappensOnlyOnce()
        {
            CheckVariableExpansionInKey("$(swix.var.My)", "$(swix.var.My)", "$(swix.var.My)");
        }

        [Test]
        public void VariableExpansion_AfterExpansionRegexGivesAnotherMatch_ExpansionHappensOnlyOnce()
        {
            CheckVariableExpansionInKey("$(swix.var.My).My)", "$(swix.var", "$(swix.var.My)");
        }

        [Test]
        public void VariableExpansion_TwoVarReferences_BothAreExpanded()
        {
            CheckVariableExpansionInKey("$(swix.var.My)+$(swix.var.My)", "value", "value+value");
        }

        private static void CheckVariableExpansionInKey(string rawAttributeValue, string varValue, string expectedAttributeExpansion)
        {
            var sut = new SemContextStub();
            sut.Variables["My"] = varValue;
            bool itemFuncCalled = false;
            sut.ItemFunc = (s, context) =>
                {
                    itemFuncCalled = true;
                    Assert.AreEqual(expectedAttributeExpansion, s);
                    return null;
                };
            sut.PushLine(0, null, rawAttributeValue, new AhlAttribute[0]);
            Assert.IsTrue(itemFuncCalled);
        }

        [Test]
        [ExpectedException(typeof (SwixSemanticException), ExpectedMessage = "'unknown' is unknown variable group")]
        public void VariableExpansion_UnknownGroup_ExceptionThrown()
        {
            var sut = new SemContextStub();
            sut.PushLine(0, null, "$(swix.unknown.var)", new AhlAttribute[0]);
        }

        [Test]
        [ExpectedException(typeof (SwixSemanticException), ExpectedMessage = "Variable 'var' is undefined")]
        public void VariableExpansion_UndeclaredVariable_ExceptionThrown()
        {
            var sut = new SemContextStub();
            sut.PushLine(0, null, "$(swix.var.var)", new AhlAttribute[0]);
        }

        [Test]
        public void VariableExpansion_AttributeExpansionWorks()
        {
            var sut = new SemContextStub();
            sut.Variables["My"] = "value";
            bool itemFuncCalled = false;
            sut.ItemFunc = (s, context) =>
                {
                    itemFuncCalled = true;
                    Assert.AreEqual("value+something+value", context.GetInheritedAttribute("a"));
                    return null;
                };
            sut.PushLine(0, null, "key", new[] { new AhlAttribute("a", "$(swix.var.My)+something+$(swix.var.My)") });
            Assert.IsTrue(itemFuncCalled);
        }

        [Test]
        public void VariableExpansion_EnvironmentVariableInKey_Expanded()
        {
            CheckEnvironmentVariableExpansionInKey("value", "$(swix.env.Test)+something", "value+something");
        }

        private static void CheckEnvironmentVariableExpansionInKey(string varValue, string rawAttributeValue, string expectedAttributeExpansion)
        {
            if (Environment.GetEnvironmentVariables().Contains("Test"))
                Assert.Inconclusive("Environment variable with name 'test' already exists");
            Environment.SetEnvironmentVariable("Test", varValue);
            try
            {
                var sut = new SemContextStub();
                bool itemFuncCalled = false;
                sut.ItemFunc = (s, context) =>
                    {
                        itemFuncCalled = true;
                        Assert.AreEqual(expectedAttributeExpansion, s);
                        return null;
                    };
                sut.PushLine(0, null, rawAttributeValue, new AhlAttribute[0]);
                Assert.IsTrue(itemFuncCalled);
            }
            finally
            {
                Environment.SetEnvironmentVariable("Test", "");
            }
        }

        [Test]
        [ExpectedException(typeof(SwixSemanticException), ExpectedMessage = "Environment variable 'Test' is undefined")]
        public void VariableExpansion_EnvironmentVariableDoesNotExist_ExceptionThrown()
        {
            if (Environment.GetEnvironmentVariables().Contains("Test"))
                Assert.Inconclusive("Environment variable with name 'test' already exists");
            var sut = new SemContextStub();
            sut.PushLine(0, null, "$(swix.env.Test)", new AhlAttribute[0]);
        }
    }
}