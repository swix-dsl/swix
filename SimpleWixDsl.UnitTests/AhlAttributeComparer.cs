﻿using System;
using System.Collections;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.UnitTests
{
    public class AhlAttributeComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var a = x as AhlAttribute;
            var b = y as AhlAttribute;
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (a.Key == b.Key && a.Value == b.Value) return 0;

            int keyComparison = String.CompareOrdinal(a.Key, b.Key);
            return keyComparison != 0 ? keyComparison : String.CompareOrdinal(a.Value, b.Value);
        }
    }
}