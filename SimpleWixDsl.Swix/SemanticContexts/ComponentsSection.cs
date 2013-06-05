using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentsSection : BaseSwixSemanticContext
    {
        private readonly List<WixComponent> _components;
        private readonly List<WixComponent> _toAdd;

        public ComponentsSection(IAttributeContext attributeContext, List<WixComponent> components) 
            : base(attributeContext)
        {
            _components = components;
            _toAdd = new List<WixComponent>();
        }

        [ItemHandler]
        public ISemanticContext Component(string key, IAttributeContext itemContext)
        {
            return new StubSwixElement(CurrentAttributeContext, () =>
                {
                    try
                    {
                        var component = WixComponent.FromContext(key, itemContext);
                        _toAdd.Add(component);
                    }
                    catch (SwixSemanticException e)
                    {
                        throw new SwixSemanticException(FormatError(e.Message));
                    }
                });
        }

        protected override void FinishItemCore()
        {
            _components.AddRange(_toAdd);
        }
    }
}