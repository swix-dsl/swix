using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentsSection : BaseSwixSemanticContext
    {
        private readonly List<WixComponent> _components;
        private readonly List<WixComponent> _toAdd;

        public ComponentsSection(int line, IAttributeContext attributeContext, List<WixComponent> components)
            : base(line, attributeContext)
        {
            _components = components;
            _toAdd = new List<WixComponent>();
        }

        protected List<WixComponent> GatheredComponents
        {
            get { return _toAdd; }
        }

        [ItemHandler]
        public virtual ISemanticContext Component(string key, IAttributeContext itemContext)
        {
            try
            {
                var component = WixComponent.FromContext(key, itemContext);
                var itemSemanticContext = new ComponentItem(CurrentLine, CurrentAttributeContext, component);
                itemSemanticContext.OnFinished += (s, e) => _toAdd.Add(component);
                return itemSemanticContext;
            }
            catch (SwixSemanticException e)
            {
                throw new SwixSemanticException(CurrentLine, e.Message);
            }
        }

        [MetaHandler("harvest")]
        public ISemanticContext HandleMetaHarvest(string key, IAttributeContext metaContext)
        {
            return new HarvestSemanticContext(CurrentLine, key, metaContext, _toAdd);
        }

        protected override void FinishItemCore()
        {
            _components.AddRange(_toAdd);
        }
    }
}