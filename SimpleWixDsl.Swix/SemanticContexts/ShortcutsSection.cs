using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ShortcutsSection : BaseSwixSemanticContext
    {
        private readonly List<Shortcut> _shortcuts;
        private readonly List<Shortcut> _toAdd;

        public ShortcutsSection(int line, IAttributeContext attributeContext, List<Shortcut> shortcuts) 
            : base(line, attributeContext)
        {
            _shortcuts = shortcuts;
            _toAdd = new List<Shortcut>();
        }

        [ItemHandler]
        public ISemanticContext HandleShortcut(string key, IAttributeContext attributes)
        {
            return new StubSwixElement(CurrentLine, attributes, () =>
                {
                    try
                    {
                        _toAdd.Add(Shortcut.FromContext(key, attributes));
                    }
                    catch (SwixItemParsingException e)
                    {
                        throw new SwixSemanticException(CurrentLine, e.Message);
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