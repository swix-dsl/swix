using System;

namespace SimpleWixDsl.Swix
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SectionHandlerAttribute : Attribute
    {
        public string SectionName { get; set; }

        public SectionHandlerAttribute(string sectionName)
        {
            SectionName = sectionName;
        }
    }
}