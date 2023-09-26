using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WowBot
{
    internal class BotStarter
    {
        private readonly InputManager inputManager;
        private readonly bool isAcore = true; 
        internal BotStarter(bool isAcore, InputManager inputManager)
        {
            this.inputManager = inputManager;
            this.isAcore = isAcore;
        }

        internal void StartBotIfNotRunning()
        {
            string processName = "Wow";
            if (!ProcessHandler.IsProcessRunning(processName))
            {
                Console.WriteLine($"Process {processName} is NOT running... Starting it!");
                ProcessHandler.StartProcess("D:\\My files\\svea_laptop\\code_hdd\\ml\\BloogBot\\Bot\\Bootstrapper.exe");
                Thread.Sleep(23000); // Wait 15s
                inputManager.SendEnter(); // Get rid of VS debug message
                Thread.Sleep(8000);
                WindowFinder.ShowWindow("World of Warcraft");
                inputManager.SendLogin(isAcore, false);
                Thread.Sleep(7000);
                WindowFinder.ShowWindow("BloogBot");
                inputManager.SendTab();
                Thread.Sleep(1000);
                inputManager.SendTab();
                Thread.Sleep(1000);
                inputManager.SendEnter();
                Thread.Sleep(3000);
                WindowFinder.ShowWindow("World of Warcraft");
            }
            else
                Console.WriteLine($"Process {processName} is running!");
        }
    }
}
