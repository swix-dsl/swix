using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class FileSemanticContext : ISemanticContext
    {
        public FileSemanticContext(SwixModel result)
        {
            throw new System.NotImplementedException();
        }

        public ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<Attribute> attributes)
        {
            throw new System.NotImplementedException();
        }

        public void FinishItem()
        {
            throw new System.NotImplementedException();
        }
    }
}