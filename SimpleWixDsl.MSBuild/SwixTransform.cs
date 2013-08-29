using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.MSBuild
{
    public class SwixTransform : Task
    {
        private static readonly Regex VarDeclaration = new Regex(@"(?<name>\w+)=(?<value>.*)", RegexOptions.Compiled);

        public SwixTransform()
        {
            GuidMode = SwixGuidMode.TreatAbsentGuidAsError.ToString();
        }

        [Required]
        public string Source { get; set; }

        public string VariablesDefinitions { get; set; }
        
        public string GuidMode { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "Transforming {0}...", Source);
                var variables = ParseVariablesDefinitions();
                SwixProcessor.Transform(Source, (SwixGuidMode) Enum.Parse(typeof(SwixGuidMode), GuidMode), variables);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
            return true;
        }

        private Dictionary<string, string> ParseVariablesDefinitions()
        {
            var result = new Dictionary<string, string>();
            if (VariablesDefinitions == null)
                return result;
            var declarations = SplitVarDeclarations();
            foreach (var declString in declarations)
            {
                var match = VarDeclaration.Match(declString);
                if (!match.Success)
                    throw new ArgumentException("Invalid VariablesDefinitions string: declaration '" + declString + "' is incorrect");
                result.Add(match.Groups["name"].Value, match.Groups["value"].Value);
            }
            return result;
        }

        private IEnumerable<string> SplitVarDeclarations()
        {
            bool lastSymbolWasEscape = false;
            var current = new StringBuilder();
            for (int i = 0; i < VariablesDefinitions.Length; i++)
            {
                var ch = VariablesDefinitions[i];
                if (lastSymbolWasEscape)
                {
                    if (ch == ';' || ch == '\\')
                        current.Append(ch);
                    else
                        current.Append('\\').Append(ch);
                    lastSymbolWasEscape = false;
                    continue;
                }
                switch (ch)
                {
                    case '\\':
                        if (i == VariablesDefinitions.Length - 1)
                            throw new ArgumentException("Invalid VariablesDefinitions string: '\\' can't be the last symbol");
                        lastSymbolWasEscape = true;
                        continue;
                    case ';':
                        yield return current.ToString();
                        current.Clear();
                        break;
                    default:
                        current.Append(ch);
                        break;
                }
            }
            yield return current.ToString();
        }
    }
}