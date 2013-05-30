using System;
using System.Collections;
using Attribute = SimpleWixDsl.Swix.Attribute;

namespace SimpleWixDsl.UnitTests
{
    public class AttributeComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var a = x as Attribute;
            var b = y as Attribute;
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (a.Key == b.Key && a.Value == b.Value) return 0;

            int keyComparison = String.CompareOrdinal(a.Key, b.Key);
            return keyComparison != 0 ? keyComparison : String.CompareOrdinal(a.Value, b.Value);
        }
    }
}