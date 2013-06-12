using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class FileSemanticContext : BaseSwixSemanticContext
    {
        private readonly SwixModel _result;

        public FileSemanticContext(SwixModel result)
            : base(new AttributeContext(new Dictionary<string, string>()))
        {
            _result = result;
        }

        public void SetPredefinedSwixVariables(IDictionary<string, string> vars)
        {
            foreach (var pair in vars)
                CurrentAttributeContext.SwixVariableDefinitions.Add(pair);
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