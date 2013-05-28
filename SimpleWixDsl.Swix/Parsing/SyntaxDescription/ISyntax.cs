using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Swix.Parsing.SyntaxDescription
{
    public interface ISyntax
    {
        IItemSyntax ChildItem { get; }
        IEnumerable<ISectionSyntax> Sections { get; }
    }

    public interface IItemSyntax : ISyntax
    {
        IAttributeSyntax Key { get; }
        IEnumerable<IAttributeSyntax> Attributes { get; }
    }

    public interface ISectionSyntax : ISyntax
    {
        string Keyword { get; }
    }

    public interface IAttributeSyntax
    {
        string Name { get; }
        bool IsMandatory { get; }
        string DefaultValue { get; }
        Func<string, bool> Validation { get; }
    }
}
