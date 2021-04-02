using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentItem : BaseSwixSemanticContext
    {
        private readonly WixComponent _component;

        public ComponentItem(int line, IAttributeContext inheritedContext, WixComponent component)
            : base(line, inheritedContext)
        {
            _component = component;
        }

        [SectionHandler("shortcuts")]
        public ISemanticContext Shortcuts(IAttributeContext sectionContext)
        {
            return new ShortcutsSection(CurrentLine, sectionContext, _component.Shortcuts);
        }

        [SectionHandler("services")]
        public ISemanticContext Services(IAttributeContext sectionContext)
        {
            return new ServicesSection(CurrentLine, sectionContext, _component.Services);
        }
    }
}
