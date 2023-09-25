using System;

namespace WowBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Dfs.StartDfs(); // DFS search wander nodes
            ProcessHandler processHandler = new ProcessHandler();
            WindowFinder windowFinder = new WindowFinder();

            bool useBloogBot = false;
            if (useBloogBot)
            {
                processHandler.StartBotIfNotRunning();
                windowFinder.ShowWindow("World of Warcraft");
            }
            else
            {
                WowBot wowBot = new WowBot();
                windowFinder.ShowWindow("World of Warcraft");
                wowBot.StartBot();
            }
            Console.ReadLine();
        }
    }
}
