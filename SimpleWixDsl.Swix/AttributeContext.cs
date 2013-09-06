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

            public string GetInheritedAttribute(string attributeName)
            {
                return null;
            }

            public void SetAttributes(IEnumerable<AhlAttribute> attributes)
            {
                throw new NotSupportedException();
            }

            public void SetAttributes(IDictionary<string, string> attributes)
            {
                throw new NotSupportedException();
            }

            public IDictionary<string, string> GetDirectlySetAttributes()
            {
                return _emptyDictionary;
            }

            public IDictionary<string, string> SwixVariableDefinitions
            {
                get { return null; }
            }

            public IEnumerable<string> GetDirectlySetUnusedAttributeNames()
            {
                return Enumerable.Empty<string>();
            }
        }

        private class AttributeValueHolder
        {
            private string _value;
            private bool _isUsed;

            public AttributeValueHolder(string value)
            {
                _value = value;
            }

            public string GetValue()
            {
                _isUsed = true;
                return _value;
            }

            public void SetValue(string value)
            {
                _value = value;
            }

            public bool IsUsed
            {
                get { return _isUsed; }
            }
        }

        private static readonly NullAttributeContext _nullAttributeContext = new NullAttributeContext();
        private readonly Dictionary<string, AttributeValueHolder> _attributes = new Dictionary<string, AttributeValueHolder>();
        private readonly IAttributeContext _parentContext;

        public AttributeContext(IDictionary<string, string> swixVariableDefinitions)
            : this(_nullAttributeContext)
        {
            SwixVariableDefinitions = swixVariableDefinitions;
        }

        public AttributeContext(IAttributeContext parentContext)
        {
            if (parentContext == null) throw new ArgumentNullException("parentContext");
            SwixVariableDefinitions = parentContext.SwixVariableDefinitions;
            _parentContext = parentContext;
        }

        public string GetInheritedAttribute(string attributeName)
        {
            AttributeValueHolder result;
            if (_attributes.TryGetValue(attributeName, out result))
                return result.GetValue();
            return _parentContext.GetInheritedAttribute(attributeName);
        }

        public void SetAttributes(IEnumerable<AhlAttribute> attributes)
        {
            foreach (var attr in attributes)
                _attributes[attr.Key] = new AttributeValueHolder(attr.Value);
        }

        public void SetAttributes(IDictionary<string, string> attributes)
        {
            foreach (var pair in attributes)
                _attributes[pair.Key] = new AttributeValueHolder(pair.Value);
        }

        public IDictionary<string, string> GetDirectlySetAttributes()
        {
            return _attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetValue());
        }

        public IDictionary<string, string> SwixVariableDefinitions { get; private set; }

        public IEnumerable<string> GetDirectlySetUnusedAttributeNames()
        {
            return _attributes.Where(kvp => !kvp.Value.IsUsed).Select(kvp => kvp.Key);
        }
    }

    public interface IAttributeContext
    {
        string GetInheritedAttribute(string attributeName);
        void SetAttributes(IEnumerable<AhlAttribute> attributes);
        void SetAttributes(IDictionary<string, string> attributes);
        IDictionary<string, string> GetDirectlySetAttributes();

        IDictionary<string, string> SwixVariableDefinitions { get; }
        IEnumerable<string> GetDirectlySetUnusedAttributeNames();
    }
}