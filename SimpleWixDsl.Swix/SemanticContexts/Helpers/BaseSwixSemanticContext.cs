using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class BaseSwixSemanticContext : ISemanticContext
    {
        private class AttributeContextFrame
        {
            public int SourceLine { get; private set; }
            public IAttributeContext Context { get; private set; }

            public AttributeContextFrame(int sourceLine, IAttributeContext context)
            {
                SourceLine = sourceLine;
                Context = context;
            }
        }

        private static readonly Regex SwixVarRegex = new Regex(@"\$\(swix\.(?<group>[a-z]+)\.(?<name>\w+)\)", RegexOptions.Compiled);
        private static readonly Regex SwixConditionRegex = new Regex(@"^\s*'(?<lhs>[^']*)'\s*(?<op>==|!=)\s*'(?<rhs>[^']*)'\s*$", RegexOptions.Compiled);
        private readonly Stack<AttributeContextFrame> _currentContexts = new Stack<AttributeContextFrame>();
        private int _currentLine;

        protected IAttributeContext CurrentAttributeContext
        {
            get { return _currentContexts.Peek().Context; }
        }

        public int CurrentLine
        {
            get { return _currentLine; }
        }

        public BaseSwixSemanticContext(int sourceLine, IAttributeContext attributeContext)
        {
            _currentContexts.Push(new AttributeContextFrame(sourceLine, attributeContext));
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
                    throw new SwixSemanticException(CurrentLine, string.Format("Section {0} is not allowed here according to SWIX format.", new[] {sectionName}));
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
                {
                    throw new SwixSemanticException(CurrentLine, "Section can't have key element");
                }
                var sectionName = keyword.Substring(1);
                var sectionHandler = reflectionInfo.GetSectionHandler(sectionName);
                if (sectionHandler == null)
                    throw new SwixSemanticException(CurrentLine, string.Format("Section {0} is not allowed here according to SWIX format.", new[] { sectionName }));
                return sectionHandler(this, childContext);
            }

            if (keyword[0] == '?')
            {
                var metaName = keyword.Substring(1);
                var metaHandler = reflectionInfo.GetMetaHandler(metaName);
                if (metaHandler == null)
                {
                    throw new SwixSemanticException(CurrentLine, string.Format("Meta {0} is not allowed here according to SWIX format.", metaName));
                }
                return metaHandler(this, key, childContext);
            }

            throw new SwixSemanticException(CurrentLine, "SWIX keywords are only prepended by :, ! or ?");
        }

        [MetaHandler("set")]
        public ISemanticContext HandleMetaSet(string key, IAttributeContext metaContext)
        {
            if (key != null)
            {
                throw new SwixSemanticException(CurrentLine, "Meta-keyword 'set' doesn't allow key attribute");
            }
            _currentContexts.Push(new AttributeContextFrame(CurrentLine, metaContext));
            return this;
        }

        [MetaHandler("if")] // attributes interpreted as if they were just ?set
        public ISemanticContext HandleMetaIf(string key, IAttributeContext metaContext)
        {
            if (key == null)
            {
                throw new SwixSemanticException(CurrentLine, string.Format("Meta-keyword 'if' requires condition as a key"));
            }
            var match = SwixConditionRegex.Match(key);
            if (!match.Success)
            {
                throw new SwixSemanticException(CurrentLine, string.Format("Condition has incorrect format: {0}.", key));
            }
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
                    throw new SwixSemanticException(CurrentLine, string.Format("Unknown operator in condition: {0}", op));
            }
        }

        private ISemanticContext HandleItem(string key, IEnumerable<AhlAttribute> attributes)
        {
            var reflectionInfo = SemanticContextReflectionInfo.Get(GetType());
            var itemHandler = reflectionInfo.GetItemHandler();
            if (itemHandler == null)
            {
                throw new SwixSemanticException(CurrentLine, "Direct items in this context are not allowed in the SWIX format.");
            }
            var itemContext = CreateNewAttributeContext();
            itemContext.SetAttributes(attributes);
            return itemHandler(this, key, itemContext);
        }

        public void FinishItem()
        {
            if (_currentContexts.Count > 1)
            {
                var frame = _currentContexts.Pop();
                var unused = frame.Context.GetDirectlySetUnusedAttributeNames().ToArray();
                if (unused.Any())
                {
                    var unusedList = String.Join(", ", unused);
                    throw new SwixSemanticException(frame.SourceLine, string.Format("These attributes were set, but not used anywhere: {0}. It could indicate typo in attribute name.", unusedList));
                }
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
                        {
                            throw new SwixSemanticException(CurrentLine, string.Format("Variable '{0}' is undefined", name));
                        }
                        return varValue;
                    }
                    else if (group == "env")
                    {
                        string varValue = Environment.GetEnvironmentVariable(name);
                        if (varValue == null)
                        {
                            throw new SwixSemanticException(CurrentLine, string.Format("Environment variable '{0}' is undefined", name ));
                        }
                        return varValue;
                    }
                    throw new SwixSemanticException(CurrentLine, string.Format("'{0}' is unknown variable group", @group));
                });
        }
    }
}