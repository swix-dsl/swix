using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class CabFilesSection : BaseSwixSemanticContext
    {
        private readonly SwixModel _model;

        public CabFilesSection(int line, IAttributeContext attributeContext, SwixModel model)
            : base(line, attributeContext)
        {
            _model = model;
        }

        [ItemHandler]
        public ISemanticContext HandleFile(string key, IAttributeContext attributes)
        {
            return new StubSwixElement(CurrentLine, CurrentAttributeContext, () => _model.CabFiles.Add(CabFile.FromContext(key, attributes)));
        }
    }
}