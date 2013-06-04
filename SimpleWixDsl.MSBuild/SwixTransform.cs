using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.MSBuild
{
    public class SwixTransform : Task
    {
        [Required]
        public string Source { get; set; }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, "Transforming {0}...", Source);
                SwixProcessor.Transform(Source);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }
            return true;
        }
    }
}