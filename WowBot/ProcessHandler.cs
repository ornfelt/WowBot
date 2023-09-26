using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace WowBot
{
    internal static class ProcessHandler
    {
        internal static bool IsProcessRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Any();
        }

        internal static void StartProcess(string processPath)
        {
            try
            {
                Process.Start(processPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start process. Error: {ex.Message}");
            }
        }
    }
}
