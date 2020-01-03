using System;
using System.Collections.Generic;
using System.Linq;
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

        public ITaskItem[] VariablesDefinitions { get; set; }

        [Output]
        public ITaskItem[] Files { get; set; }

        public string GuidMode { get; set; }

        public string TargetDirectory { get; set; }

        public override bool Execute()
        {
            var result = new List<ITaskItem>();
            foreach (var source in Sources)
            {
                try
                {
                    Log.LogMessage(MessageImportance.Low, "Transforming {0}...", source);
                    var variables = ParseVariablesDefinitions();
                    var varList = string.Concat(variables.Select(v => $"\n    {v.Key} = {v.Value}"));
                    Log.LogMessage(MessageImportance.Low, $"Swix variables parsed:{varList}");
                    var model = SwixProcessor.Transform(source.ItemSpec, (SwixGuidMode) Enum.Parse(typeof (SwixGuidMode), GuidMode), TargetDirectory, variables);

                    ITaskItem ToItem(WixComponent c)
                    {
                        var r = new TaskItem(c.SourcePath);
                        r.SetMetadata("Tag", c.OutputTag);
                        return r;
                    }

                    result.AddRange(model.Components.Where(c => !string.IsNullOrEmpty(c.OutputTag)).Select(ToItem));
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

            Files = result.ToArray();
            return true;
        }

        private Dictionary<string, string> ParseVariablesDefinitions()
        {
            var result = new Dictionary<string, string>();
            if (VariablesDefinitions == null)
                return result;
            var declarations = VariablesDefinitions.Select(d => d.ItemSpec);
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
    }
}