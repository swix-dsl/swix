using System;
using System.Collections.Generic;
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
            SwixProcessor.Transform(args[0]);
        }
    }
}