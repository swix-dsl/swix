using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ServicesSection : BaseSwixSemanticContext
    {
        private readonly List<Service> _services;
        private readonly List<Service> _toAdd;

        public ServicesSection(int line, IAttributeContext attributeContext, List<Service> services) 
            : base(line, attributeContext)
        {
            _services = services;
            _toAdd = new List<Service>();
        }

        [ItemHandler]
        public ISemanticContext HandleService(string key, IAttributeContext attributes)
        {
            return new StubSwixElement(CurrentLine, attributes, () =>
                {
                    try
                    {
                        _toAdd.Add(Service.FromContext(key, attributes));
                    }
                    catch (SwixItemParsingException e)
                    {
                        throw new SwixSemanticException(CurrentLine, e.Message);
                    }
                });
        }

        protected override void FinishItemCore()
        {
            base.FinishItemCore();
            _services.AddRange(_toAdd);
        }
    }
}