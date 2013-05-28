namespace SimpleWixDsl.Ahl.Parsing
{
    /// <summary>
    /// Determines current parsing rules: each section could have different types of children items or sections, 
    /// thus it seems natural to organize entire parsing process as State Design Pattern.
    /// </summary>
    public interface IParsingState
    {
        /// <summary>
        /// Tries to parse next source line in this context. Returns if this try succeeded. 
        /// </summary>
        bool TryParseNextLine(LineLexem nextLine);
    }
}