using System;

namespace SimpleWixDsl.Swix
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetaHandlerAttribute : Attribute
    {
        public string MetaName { get; set; }

        public MetaHandlerAttribute(string metaName)
        {
            MetaName = metaName;
        }
    }
}