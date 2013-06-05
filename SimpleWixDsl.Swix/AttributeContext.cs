using System;
using System.Collections.Generic;
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
        }

        private static readonly NullAttributeContext _nullAttributeContext = new NullAttributeContext();
        private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();
        private readonly IAttributeContext _parentContext;

        public AttributeContext()
            :this(_nullAttributeContext)
        {
        }

        public AttributeContext(IAttributeContext parentContext)
        {
            if (parentContext == null) throw new ArgumentNullException("parentContext");
            _parentContext = parentContext;
        }

        public string GetInheritedAttribute(string attributeName)
        {
            string result;
            if (_attributes.TryGetValue(attributeName, out result))
                return result;
            return _parentContext.GetInheritedAttribute(attributeName);
        }

        public void SetAttributes(IEnumerable<AhlAttribute> attributes)
        {
            foreach (var attr in attributes)
                _attributes[attr.Key] = attr.Value;
        }

        public void SetAttributes(IDictionary<string, string> attributes)
        {
            foreach (var pair in attributes)
            {
                _attributes[pair.Key] = pair.Value;
            }
        }

        public IDictionary<string, string> GetDirectlySetAttributes()
        {
            return _attributes;
        }
    }

    public interface IAttributeContext
    {
        string GetInheritedAttribute(string attributeName);
        void SetAttributes(IEnumerable<AhlAttribute> attributes);
        void SetAttributes(IDictionary<string, string> attributes);
        IDictionary<string, string> GetDirectlySetAttributes();
    }
}