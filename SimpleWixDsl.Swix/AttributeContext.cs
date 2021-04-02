using System;
using System.Collections.Generic;
using System.Linq;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class AttributeContext : IAttributeContext
    {
        private class NullAttributeContext : IAttributeContext
        {
            private readonly Dictionary<string, string> _emptyDictionary = new Dictionary<string, string>();
            public string GetInheritedAttribute(string attributeName) => null;
            public void SetAttributes(IEnumerable<AhlAttribute> attributes) => throw new NotSupportedException();
            public IDictionary<string, string> GetDirectlySetAttributes() => _emptyDictionary;
            public IDictionary<string, string> SwixVariableDefinitions => null;
            public GuidProvider GuidProvider => null;
            public IEnumerable<string> GetDirectlySetUnusedAttributeNames() => Enumerable.Empty<string>();
        }

        private class AttributeValueHolder
        {
            private readonly string _value;

            public AttributeValueHolder(string value) => _value = value;

            public string GetValue()
            {
                IsUsed = true;
                return _value;
            }

            public bool IsUsed { get; private set; }
        }

        private static readonly NullAttributeContext _nullAttributeContext = new NullAttributeContext();
        private readonly Dictionary<string, AttributeValueHolder> _attributes = new Dictionary<string, AttributeValueHolder>();
        private readonly IAttributeContext _parentContext;

        public AttributeContext(IDictionary<string, string> swixVariableDefinitions, GuidProvider guidProvider)
            : this(_nullAttributeContext)
        {
            SwixVariableDefinitions = swixVariableDefinitions;
            GuidProvider = guidProvider;
        }

        public AttributeContext(IAttributeContext parentContext)
        {
            if (parentContext == null) throw new ArgumentNullException(nameof(parentContext));
            SwixVariableDefinitions = parentContext.SwixVariableDefinitions;
            _parentContext = parentContext;
            GuidProvider = parentContext.GuidProvider;
        }

        public string GetInheritedAttribute(string attributeName)
        {
            if (_attributes.TryGetValue(attributeName, out var result))
                return result.GetValue();
            return _parentContext.GetInheritedAttribute(attributeName);
        }

        public void SetAttributes(IEnumerable<AhlAttribute> attributes)
        {
            foreach (var attr in attributes)
                _attributes[attr.Key] = new AttributeValueHolder(attr.Value);
        }

        public IDictionary<string, string> GetDirectlySetAttributes()
        {
            return _attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetValue());
        }

        public IDictionary<string, string> SwixVariableDefinitions { get; }
        public GuidProvider GuidProvider { get; }

        public IEnumerable<string> GetDirectlySetUnusedAttributeNames()
        {
            return _attributes.Where(kvp => !kvp.Value.IsUsed).Select(kvp => kvp.Key);
        }
    }

    public interface IAttributeContext
    {
        string GetInheritedAttribute(string attributeName);
        void SetAttributes(IEnumerable<AhlAttribute> attributes);
        IDictionary<string, string> GetDirectlySetAttributes();

        IDictionary<string, string> SwixVariableDefinitions { get; }
        GuidProvider GuidProvider { get; }
        IEnumerable<string> GetDirectlySetUnusedAttributeNames();
    }
}
