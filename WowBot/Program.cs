using System;
using System.Timers;

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

                // Create a timer with a 5-minute interval (in milliseconds)
                Timer timer = new Timer(5 * 60 * 1000); // 5 minutes = 5 * 60 seconds * 1000 milliseconds
                // Hook up the Elapsed event for the timer
                timer.Elapsed += (sender, e) => TimerElapsed(sender, e, botStarter);
                // Start the timer
                timer.Start();

                // This also works...
                while (true)
                {
                    botStarter.StartBotIfNotRunning();
                    System.Threading.Thread.Sleep(300000); // 300000 milliseconds = 5 minutes
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

        private static void TimerElapsed(object sender, ElapsedEventArgs e, BotStarter botStarter)
        {
            // This method will be called every 5 minutes
            //botStarter.StartBotIfNotRunning();
        }
    }
}
