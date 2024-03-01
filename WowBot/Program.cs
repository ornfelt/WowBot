using System;
using System.Timers;

namespace WowBot
{
    internal class Program
    {
        // Usage:
        // bloogbot: start with no arguments or with 'remote' if using remote wow server
        // other wowbot: start with (isLinux, isAcore, nonLocalServerSettings), example: 1 1 or 1 1 0,80
        static void Main(string[] args)
        {
            //Dfs.StartDfs(); // DFS search wander nodes

            InputManager inputManager = new InputManager();
            bool useBloogBot = args.Length < 2;
            bool useBloogBotRemote = args.Length > 0 && args[0].ToLower().Contains("remote");
            bool isLinux = false; // Not used currently
            bool isAcore = true;
            string nonLocalServerSettings; // If non-local server, settings are provided like: 0,80 (isAlly, playerLevel)

            for (int i = 0; i < args.Length; i++)
                Console.WriteLine("arg " + i + ": " + args[i]);

            isLinux = args.Length > 0 && args[0].Contains("1");
            if (args.Length > 1) // Only update if provided as arg, else use the preset value above
                isAcore = args[1].Contains("1");
            nonLocalServerSettings = args.Length > 2 ? args[2] : "";

            if (useBloogBot)
            {
                BotStarter botStarter = new BotStarter(isAcore, useBloogBotRemote, inputManager);
                // Create a timer with a 5-minute interval (in milliseconds)
                Timer timer = new Timer(5 * 60 * 1000); // 5 minutes = 5 * 60 seconds * 1000 milliseconds
                // Hook up the Elapsed event for the timer
                timer.Elapsed += (sender, e) => TimerElapsed(sender, e, botStarter);
                // Start the timer
                timer.Start();

                // Alternative - infinite loop
                while (true)
                {
                    try
                    {
                        botStarter.StartBotIfNotRunning();
                        inputManager.SendEnterInWow(); // Get rid of crash message if any
                        inputManager.SendEnterInWow(); // Get rid of crash message if any
                        System.Threading.Thread.Sleep(300000); // 300000 milliseconds = 5 minutes
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            else
            {
                WowBot wowBot = new WowBot(isAcore, nonLocalServerSettings, inputManager);
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
            Console.WriteLine("5 minutes have passed!");
            //botStarter.StartBotIfNotRunning();
        }
    }
}
