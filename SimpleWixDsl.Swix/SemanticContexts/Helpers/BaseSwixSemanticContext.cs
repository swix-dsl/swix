using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class BaseSwixSemanticContext : ISemanticContext
    {
        private static readonly Regex SwixVarRegex = new Regex(@"\$\(swix\.(?<group>[a-z]+)\.(?<name>\w+)\)", RegexOptions.Compiled);
        private static readonly Regex SwixConditionRegex = new Regex(@"^\s*'(?<lhs>[^']*)'\s*(?<op>==|!=)\s*'(?<rhs>[^']*)'\s*$", RegexOptions.Compiled);
        private readonly Stack<IAttributeContext> _currentContexts = new Stack<IAttributeContext>();
        private int _currentLine;

        protected IAttributeContext CurrentAttributeContext
        {
            get { return _currentContexts.Peek(); }
        }

        public BaseSwixSemanticContext(IAttributeContext attributeContext)
        {
            _currentContexts.Push(attributeContext);
        }

        public ISemanticContext PushLine(int line, string keyword, string key, IEnumerable<AhlAttribute> attributes)
        {
            _currentLine = line;

            // expand variables
            key = ExpandSwixVariables(key);
            var expandedAttrs = attributes.Select(a => new AhlAttribute(a.Key, ExpandSwixVariables(a.Value)));

            if (keyword == null)
            {
                return HandleItem(key, expandedAttrs);
            }

            var reflectionInfo = SemanticContextReflectionInfo.Get(GetType());
            if (keyword[0] == '!')
            {
                var sectionName = keyword.Substring(1);
                var sectionHandler = reflectionInfo.GetSectionHandler(sectionName);
                if (sectionHandler == null)
                    throw new SwixSemanticException(FormatError("Section {0} is not allowed here according to SWIX format.", sectionName));
                var sectionContext = sectionHandler(this, CurrentAttributeContext);
                var itemContext = sectionContext.PushLine(line, null, key, expandedAttrs);
                itemContext.OnFinished += (s, e) => sectionContext.FinishItem();
                return itemContext;
            }

            var childContext = CreateNewAttributeContext();
            childContext.SetAttributes(expandedAttrs);
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
        public ISemanticContext HandleMetaSet(string key, IAttributeContext metaContext)
        {
            if (key != null)
                throw new SwixSemanticException(FormatError("Meta-keyword 'set' doesn't allow key attribute"));
            _currentContexts.Push(metaContext);
            return this;
        }

        [MetaHandler("if")] // attributes interpreted as if they were just ?set
        public ISemanticContext HandleMetaIf(string key, IAttributeContext metaContext)
        {
            if (key == null)
                throw new SwixSemanticException(FormatError("Meta-keyword 'if' requires condition as a key"));
            var match = SwixConditionRegex.Match(key);
            if (!match.Success)
                throw new SwixSemanticException(FormatError("Condition has incorrect format: {0}.", key));
            var op = match.Groups["op"].Value;
            var lhs = match.Groups["lhs"].Value;
            var rhs = match.Groups["rhs"].Value;
            switch (op)
            {
                case "==":
                    return rhs == lhs ? HandleMetaSet(null, metaContext) : new IgnoringSemanticContext();
                case "!=":
                    return rhs != lhs ? HandleMetaSet(null, metaContext) : new IgnoringSemanticContext();
                default:
                    throw new SwixSemanticException(FormatError("Unknown operator in condition: {0}", op));
            }
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
            if (_currentContexts.Count > 1)
            {
                _currentContexts.Pop();
                return;
            }
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

        protected string ExpandSwixVariables(string value)
        {
            if (value == null) return null;
            return SwixVarRegex.Replace(value, match =>
                {
                    var group = match.Groups["group"].Value;
                    var name = match.Groups["name"].Value;
                    if (group == "var")
                    {
                        string varValue;
                        if (!CurrentAttributeContext.SwixVariableDefinitions.TryGetValue(name, out varValue))
                            throw new SwixSemanticException(FormatError("Variable '{0}' is undefined", name));
                        return varValue;
                    }
                    else if (group == "env")
                    {
                        string varValue = Environment.GetEnvironmentVariable(name);
                        if (varValue == null)
                            throw new SwixSemanticException(FormatError("Environment variable '{0}' is undefined", name));
                        return varValue;
                    }
                    throw new SwixSemanticException(FormatError("'{0}' is unknown variable group", group));
                });
        }
    }
}