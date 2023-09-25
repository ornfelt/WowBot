using System;
using System.Linq;
using System.Diagnostics;

namespace WowBot
{
    internal class ProcessHandler
    {
        public void StartBotIfNotRunning()
        {
            string processName = "notepad";
            //string processName = "wow";
            if (!IsProcessRunning(processName))
            {
                Console.WriteLine($"Process {processName} is not running... Starting it!");
                StartProcess("notepad.exe");
            }
            else
                Console.WriteLine($"Process {processName} is running!");
        }

        private static bool IsProcessRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Any();
        }

        private static void StartProcess(string processPath)
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
