using System;
using System.Threading;

namespace WowBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Dfs.StartDfs(); // DFS search wander nodes
            InputManager inputManager = new InputManager();

            bool useBloogBot = true;
            bool isAcore = true;

            if (useBloogBot)
            {
                BotStarter botStarter = new BotStarter(isAcore, inputManager);
                //var startTimeSpan = TimeSpan.Zero;
                //var periodTimeSpan = TimeSpan.FromMinutes(5);
                //var timer = new System.Threading.Timer((e) =>
                //{
                //    botStarter.StartBotIfNotRunning();
                //}, null, startTimeSpan, periodTimeSpan);
                while (true)
                {
                    botStarter.StartBotIfNotRunning();
                    Thread.Sleep(120000); // 120000 milliseconds = 2 minutes
                }
            }
            else
            {
                WowBot wowBot = new WowBot(isAcore, inputManager);
                WindowFinder.ShowWindow("World of Warcraft");
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine($"Current window: {WindowFinder.GetCurrentWindow()}");
                wowBot.StartBot();
            }
            System.Threading.Thread.Sleep(3000);
        }
    }
}
