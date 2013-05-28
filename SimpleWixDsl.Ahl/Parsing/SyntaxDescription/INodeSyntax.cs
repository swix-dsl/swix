using System;
using System.Collections.Generic;

namespace SimpleWixDsl.Ahl.Parsing
{
    public class ParseProcessEventArgs : EventArgs
    {
        public ParseProcessEventArgs(ParsedElement target)
        {
            Target = target;
        }

        public ParsedElement Target { get; private set; }
    }

    public interface IElementSyntax
    {
        event EventHandler<ParseProcessEventArgs> ParseStarted;
        event EventHandler<ParseProcessEventArgs> ParseFinished;
    }

    internal interface IElementSyntaxCore : IElementSyntax
    {
        void RaiseParseStarted(ParsedElement parsed);
        void RaiseParseFinished(ParsedElement parsed);
    }

    public interface INodeSyntax : IElementSyntax
    {
        IItemSyntax ChildItem { get; }
        IEnumerable<ISectionSyntax> Sections { get; }
    }

    public interface IItemSyntax : INodeSyntax
    {
        IEnumerable<string> AttributeNames { get; }
    }

    public interface ISectionSyntax : INodeSyntax
    {
        string Keyword { get; }
    }
}