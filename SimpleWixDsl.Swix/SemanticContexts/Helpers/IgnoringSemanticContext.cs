using System;
using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class IgnoringSemanticContext : ISemanticContext
    {
        private int _nestingLevel = 0;

        public ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<AhlAttribute> attributes)
        {
            _nestingLevel++;
            return this;
        }

        public void FinishItem()
        {
            if (--_nestingLevel > 0) return;
            var handler = OnFinished;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> OnFinished;
    }
}