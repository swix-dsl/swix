using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentItem : BaseSwixSemanticContext
    {
        private readonly WixComponent _component;
        
        public ComponentItem(IAttributeContext inheritedContext, WixComponent component) 
            : base(inheritedContext)
        {
            _component = component;
        }

        [SectionHandler("shortcuts")]
        public ISemanticContext Shortcuts(IAttributeContext sectionContext)
        {
            return new ShortcutsSection(sectionContext, _component.Shortcuts);
        }

        [SectionHandler("services")]
        public ISemanticContext Services(IAttributeContext sectionContext)
        {
            return new ServicesSection(sectionContext, _component.Services);
        }
    }
}