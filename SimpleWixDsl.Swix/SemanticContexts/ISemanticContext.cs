using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public interface ISemanticContext
    {
        ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<Attribute> attributes);
        void FinishItem();
    }
}