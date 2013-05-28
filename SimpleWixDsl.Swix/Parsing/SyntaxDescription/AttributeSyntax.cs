﻿using System;

namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public class AttributeSyntax : IAttributeSyntax
    {
        public AttributeSyntax(string name, bool isMandatory, string defaultValue, Func<string, bool> validation)
        {
            Validation = validation;
            DefaultValue = defaultValue;
            IsMandatory = isMandatory;
            Name = name;
        }

        public string Name { get; private set; }
        public bool IsMandatory { get; private set; }
        public string DefaultValue { get; private set; }
        public Func<string, bool> Validation { get; private set; }
    }
}