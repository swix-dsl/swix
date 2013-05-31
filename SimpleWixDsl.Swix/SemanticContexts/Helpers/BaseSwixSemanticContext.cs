using System;
using System.Collections.Generic;
using System.Linq;
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

            var reflectionInfo = SemanticContextReflectionInfo.Get(GetType());

            if (keyword == null)
            {
                var itemHandler = reflectionInfo.GetItemHandler();
                if (itemHandler == null)
                    throw new SwixSemanticException(FormatError("Direct items in this context are not allowed in the SWIX format."));
                return itemHandler(this, key, attributes);
            }

            if (keyword[0] == ':')
            {
                if (key != null)
                    throw new SwixSemanticException(FormatError("Section can't have key element"));
                var sectionName = keyword.Substring(1);
                var sectionHandler = reflectionInfo.GetSectionHandler(sectionName);
                if (sectionHandler == null) 
                    throw new SwixSemanticException(FormatError("Section {0} is not allowed here according to SWIX format.", sectionName));
                return sectionHandler(this, attributes);
            }

            if (keyword[0] == '!')
            {
                var sectionName = keyword.Substring(1);
                var sectionHandler = reflectionInfo.GetSectionHandler(sectionName);
                if (sectionHandler == null)
                    throw new SwixSemanticException(FormatError("Section {0} is not allowed here according to SWIX format.", sectionName));
                var sectionContext = sectionHandler(this, Enumerable.Empty<AhlAttribute>());
                var itemContext = sectionContext.PushLine(line, null, key, attributes);
                itemContext.OnFinished += (s, e) => sectionContext.FinishItem();
                return itemContext;
            }

            if (keyword[0] == '?')
            {
                var metaName = keyword.Substring(1);
                var metaHandler = reflectionInfo.GetMetaHandler(metaName);
                if (metaHandler == null)
                    throw new SwixSemanticException(FormatError("Meta {0} is not allowed here according to SWIX format.", metaName));
                return metaHandler(this, key, attributes);
            }

            throw new SwixSemanticException(FormatError("SWIX keywords are only prepended by :, ! or ?"));
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
    }
}
