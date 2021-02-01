using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TSMPModdedSIILauncher.Core;

namespace TSMPModdedSIILauncher.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var consoleLauncher = new ConsoleLauncher();
            consoleLauncher.Initialize();
            consoleLauncher.Start();
        }
    }
}
