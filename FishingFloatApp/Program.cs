using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FishingFloatApp
{
    public class ConsoleHelper
    {
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
    }

}