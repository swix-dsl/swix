using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class FileSemanticContext : BaseSwixSemanticContext
    {
        private readonly SwixModel _result;

        public FileSemanticContext(SwixModel result)
            :base(new AttributeContext())
        {
            _result = result;
        }

        [SectionHandler("cabFiles")]
        public ISemanticContext CabFiles(IEnumerable<AhlAttribute> attributes)
        {
            return null;
        }
    }
}