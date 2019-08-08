using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SimpleWixDsl.Ahl;
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
        public ITaskItem[] Sources { get; set; }

        public string VariablesDefinitions { get; set; }

        public string GuidMode { get; set; }

        public string TargetDirectory { get; set; }

        public override bool Execute()
        {
            foreach (var source in Sources)
            {
                try
                {
                    Log.LogMessage(MessageImportance.Low, "Transforming {0}...", source);
                    var variables = ParseVariablesDefinitions();
                    var varList = string.Concat(variables.Select(v => $"\n    {v.Key} = {v.Value}"));
                    Log.LogMessage(MessageImportance.Low, $"Swix variables parsed:{varList}");
                    SwixProcessor.Transform(source.ItemSpec, (SwixGuidMode) Enum.Parse(typeof (SwixGuidMode), GuidMode), TargetDirectory, variables);
                }
                catch (SourceCodeException e)
                {
                    var file = source.GetMetadata("FullPath");
                    var args = new BuildErrorEventArgs("SWIX", string.Empty, file, e.LineNumber, 0, e.LineNumber, 0, e.Message, string.Empty, string.Empty);

                    BuildEngine.LogErrorEvent(args);
                    return false;
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                    return false;
                }
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
                if (String.IsNullOrWhiteSpace(declString))
                    continue;
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
                            current.Append('\\');
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