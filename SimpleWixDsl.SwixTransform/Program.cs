using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleWixDsl.Swix;

namespace SimpleWixDsl.SwixTransform
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var targetPath = Path.GetDirectoryName(args[0]);
            SwixProcessor.Transform(args[0], SwixGuidMode.UseExistingAndUpdateStorage, targetPath);
        }
    }
}