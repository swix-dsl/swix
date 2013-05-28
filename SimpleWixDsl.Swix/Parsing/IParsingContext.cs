using System.Collections.Generic;

namespace SimpleWixDsl.Swix.Parsing
{
    /// <summary>
    /// Represents context of particular source line, i.e. defaults for all attributes that were set on this level, above this level or globally.
    /// </summary>
    public interface IParsingContext
    {
        Dictionary<string, string> DefaultAttributes { get; } 
    }
}