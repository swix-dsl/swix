using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ShortcutsSection : BaseSwixSemanticContext
    {
        private readonly List<Shortcut> _shortcuts;
        private readonly List<Shortcut> _toAdd;

        public ShortcutsSection(IAttributeContext attributeContext, List<Shortcut> shortcuts) 
            : base(attributeContext)
        {
            _shortcuts = shortcuts;
            _toAdd = new List<Shortcut>();
        }

        [ItemHandler]
        public ISemanticContext HandleShortcut(string key, IAttributeContext attributes)
        {
            return new StubSwixElement(attributes, () =>
                {
                    try
                    {
                        _toAdd.Add(Shortcut.FromContext(key, attributes));
                    }
                    catch (SwixSemanticException e)
                    {
                        throw new SwixSemanticException(FormatError(e.Message));
                    }
                });
        }

        protected override void FinishItemCore()
        {
            base.FinishItemCore();
            _shortcuts.AddRange(_toAdd);
        }
    }
}