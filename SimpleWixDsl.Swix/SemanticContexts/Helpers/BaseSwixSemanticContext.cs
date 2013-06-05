using System;
using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class BaseSwixSemanticContext : ISemanticContext
    {
        private int _currentLine;
        protected IAttributeContext CurrentAttributeContext { get; set; }

        public BaseSwixSemanticContext(IAttributeContext attributeContext)
        {
            CurrentAttributeContext = attributeContext;
        }

        public ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<AhlAttribute> attributes)
        {
            _currentLine = line;

            if (keyword == null)
            {
                return HandleItem(key, attributes);
            }

            var reflectionInfo = SemanticContextReflectionInfo.Get(GetType());
            if (keyword[0] == '!')
            {
                var sectionName = keyword.Substring(1);
                var sectionHandler = reflectionInfo.GetSectionHandler(sectionName);
                if (sectionHandler == null)
                    throw new SwixSemanticException(FormatError("Section {0} is not allowed here according to SWIX format.", sectionName));
                var sectionContext = sectionHandler(this, CurrentAttributeContext);
                var itemContext = sectionContext.PushLine(line, null, key, attributes);
                itemContext.OnFinished += (s, e) => sectionContext.FinishItem();
                return itemContext;
            }

            var childContext = CreateNewAttributeContext();
            childContext.SetAttributes(attributes);
            if (keyword[0] == ':')
            {
                if (key != null)
                    throw new SwixSemanticException(FormatError("Section can't have key element"));
                var sectionName = keyword.Substring(1);
                var sectionHandler = reflectionInfo.GetSectionHandler(sectionName);
                if (sectionHandler == null) 
                    throw new SwixSemanticException(FormatError("Section {0} is not allowed here according to SWIX format.", sectionName));
                return sectionHandler(this, childContext);
            }

            if (keyword[0] == '?')
            {
                var metaName = keyword.Substring(1);
                var metaHandler = reflectionInfo.GetMetaHandler(metaName);
                if (metaHandler == null)
                    throw new SwixSemanticException(FormatError("Meta {0} is not allowed here according to SWIX format.", metaName));
                return metaHandler(this, key, childContext);
            }

            throw new SwixSemanticException(FormatError("SWIX keywords are only prepended by :, ! or ?"));
        }

        [MetaHandler("set")]
        public ISemanticContext HandleMeta(string key, IAttributeContext metaContext)
        {
            if (key != null)
                throw new SwixSemanticException("Meta-keyword set doesn't allow key attribute");
            return new StubSwixElement(metaContext, () => CurrentAttributeContext.SetAttributes(metaContext.GetDirectlySetAttributes()));
        }

        private ISemanticContext HandleItem(string key, IEnumerable<AhlAttribute> attributes)
        {
            var reflectionInfo = SemanticContextReflectionInfo.Get(GetType());
            var itemHandler = reflectionInfo.GetItemHandler();
            if (itemHandler == null)
                throw new SwixSemanticException(FormatError("Direct items in this context are not allowed in the SWIX format."));
            var itemContext = CreateNewAttributeContext();
            itemContext.SetAttributes(attributes);
            return itemHandler(this, key, itemContext);
        }

        public void FinishItem()
        {
            FinishItemCore();
            var handler = OnFinished;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void FinishItemCore()
        {
        }

        public event EventHandler<EventArgs> OnFinished;

        protected string FormatError(string format, params object[] args)
        {
            var userMsg = string.Format(format, args);
            return string.Format("Line {0}: {1}", _currentLine, userMsg);
        }

        protected virtual IAttributeContext CreateNewAttributeContext()
        {
            return new AttributeContext(CurrentAttributeContext);
        }
    }
}
