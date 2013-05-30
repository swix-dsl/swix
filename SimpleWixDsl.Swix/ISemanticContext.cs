using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public interface ISemanticContext
    {
        ISemanticContext AddChild(string key, IEnumerable<Attribute> attributes);
        void FinishItem();
    }
}