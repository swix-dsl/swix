using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public interface ISemanticContext
    {
        ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<AhlAttribute> attributes);
        void FinishItem();
    }
}