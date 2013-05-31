using System;

namespace SimpleWixDsl.Swix
{
    /// <summary>
    /// Element that doesn't allow any subsections or subitems
    /// </summary>
    public class StubSwixElement : BaseSwixSemanticContext
    {
        private readonly Action _onFinish;

        public StubSwixElement(IAttributeContext attributeContext, Action onFinish)
            : base(attributeContext)
        {
            _onFinish = onFinish;
        }

        protected override void FinishItemCore()
        {
            if (_onFinish != null)
                _onFinish();
        }
    }
}