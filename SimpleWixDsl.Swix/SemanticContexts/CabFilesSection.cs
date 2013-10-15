using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class CabFilesSection : BaseSwixSemanticContext
    {
        private readonly SwixModel _model;
        private int _diskIdsStartFrom;

        public CabFilesSection(int line, IAttributeContext attributeContext, SwixModel model)
            : base(line, attributeContext)
        {
            _model = model;
        }

        [ItemHandler]
        public ISemanticContext HandleFile(string key, IAttributeContext attributes)
        {
            string startFromStr = attributes.GetInheritedAttribute("startFrom") ?? "1";
            int startFrom;
            if (!int.TryParse(startFromStr, out startFrom))
                throw new SwixSemanticException(CurrentLine, "Can't parse startFrom number for cabFiles section");
            _diskIdsStartFrom = startFrom;

            return new StubSwixElement(CurrentLine, CurrentAttributeContext, () =>
            {
                try
                {
                    var cabFile = CabFile.FromContext(key, attributes);
                    _model.CabFiles.Add(cabFile);
                }
                catch (SwixItemParsingException e)
                {
                    throw new SwixSemanticException(CurrentLine, string.Format("{0}", new[] { e.Message }));
                }
            });
        }

        protected override void FinishItemCore()
        {
            _model.DiskIdStartFrom = _diskIdsStartFrom;
            base.FinishItemCore();
        }
    }
}