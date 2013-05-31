using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Ahl
{
    public interface ISemanticContext
    {
        ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<AhlAttribute> attributes);
        void FinishItem();
        event EventHandler<EventArgs> OnFinished;
    }
}