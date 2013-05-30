﻿using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public interface IParsingContext
    {
        void PushLine(int lineNumber, int indent, string keyword, string key, IEnumerable<Attribute> attributes);        
    }
}