using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class CabFilesSection : BaseSwixSemanticContext
    {
        private readonly SwixModel _model;

        public CabFilesSection(IAttributeContext attributeContext, SwixModel model)
            : base(attributeContext)
        {
            _model = model;
        }

        [ItemHandler]
        public ISemanticContext HandleFile(string key, IAttributeContext attributes)
        {
            return new StubSwixElement(CurrentAttributeContext, () => _model.CabFiles.Add(new CabFile(key)));
        }
    }
}