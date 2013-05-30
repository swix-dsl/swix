using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class FileSemanticContext : ISemanticContext
    {
        public FileSemanticContext(SwixModel result)
        {
            throw new System.NotImplementedException();
        }

        public ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<AhlAttribute> attributes)
        {
            throw new System.NotImplementedException();
        }

        public void FinishItem()
        {
            throw new System.NotImplementedException();
        }
    }
}