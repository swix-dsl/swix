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
        public ISemanticContext CabFiles(IAttributeContext sectionContext)
        {
            return new CabFilesSection(sectionContext, _result);
        }

        [SectionHandler("directories")]
        public ISemanticContext Directories(IAttributeContext sectionContext)
        {
            return new DirectoriesSection(sectionContext, _result.RootDirectory);
        }

        [SectionHandler("components")]
        public ISemanticContext Components(IAttributeContext sectionContext)
        {
            return new ComponentsSection(sectionContext, _result.Components);
        }
    }
}