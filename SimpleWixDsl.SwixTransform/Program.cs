using System;
using System.Collections.Generic;
using System.IO;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.SwixTransform
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var targetPath = Path.GetDirectoryName(args[0]);
            var swixVars = args.Length == 1 ? null : GetArgs(args[1]);
            if (swixVars != null)
            {
                Console.WriteLine("Arguments found:");
                foreach (var variable in swixVars)
                    Console.WriteLine($"{variable.Key}={variable.Value}");
            }

            SwixProcessor.Transform(args[0], SwixGuidMode.UseExistingAndUpdateStorage, targetPath, swixVars);
        }

        private static Dictionary<string, string> GetArgs(string source)
        {
            var res = new Dictionary<string, string>();
            var split = source.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var arg in split)
            {
                var eqIdx = arg.IndexOf('=', 0);
                if (eqIdx == -1) continue;
                var name = arg.Substring(0, eqIdx);
                var val = arg.Substring(eqIdx + 1);
                res[name] = val;
            }

            return res;
        }
    }
}