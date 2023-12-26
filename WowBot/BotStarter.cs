using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
//using System.Data.SqlClient; // If you're using SQL Server
using MySql.Data.MySqlClient;

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
                StartBot();
            }
            else
            {
                Console.WriteLine($"Process {processName} is running!");
                if (!IsPlayerOnline())
                {
                    Console.WriteLine("Player not logged in. Closing Wow and starting bot again...");
                    WindowFinder.ShowWindow("World of Warcraft");
                    Thread.Sleep(5000);
                    inputManager.SendEscape();
                    Thread.Sleep(500);
                    inputManager.SendEscape();
                    Thread.Sleep(5000);
                    // Close wow via process
                    foreach (var process in Process.GetProcessesByName(processName))
                    {
                        process.Kill();
                    }
                    Thread.Sleep(5000);
                    StartBot();
                }
            }
        }

        private void StartBot()
        {
            string codeRootDir = Environment.GetEnvironmentVariable("code_root_dir");
            if (string.IsNullOrWhiteSpace(codeRootDir))
            {
                Console.WriteLine("The 'code_root_dir' environment variable is not set.");
                return;
            }

            string exePath = Path.Combine(codeRootDir, @"Code2\C#\BloogBot\Bot\Bootstrapper.exe");
            if (!File.Exists(exePath))
            {
                Console.WriteLine($"The executable file '{exePath}' does not exist.");
                return;
            }

            Console.WriteLine($"Using executable path: {exePath}");
            ProcessHandler.StartProcess(exePath);
            Thread.Sleep(25000);
            inputManager.SendEnter(); // Get rid of VS debug message
            Thread.Sleep(8000);
            WindowFinder.ShowWindow("World of Warcraft");
            inputManager.SendLogin(isAcore, false, true);
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

        private bool IsPlayerOnline()
        {
            MySqlConnection connection = null;
            Console.WriteLine("Retrieving player settings...");
            try
            {
                // Uncomment if needed
                // MySqlClientFactory.Instance.CreateConnection();
                if (isAcore)
                    connection = new MySqlConnection("Server=localhost;Database=acore_characters;User ID=acore;Password=acore;");
                else
                    connection = new MySqlConnection("Server=localhost;Database=characters;User ID=trinity;Password=trinity;");
                connection.Open();

                //int accountId = 1;
                //MySqlCommand command = new MySqlCommand($"select name, race, level from characters where online = 1 and account = {accountId}", connection);
                MySqlCommand command = new MySqlCommand($"select name, race, level from characters where online = 1", connection);
                MySqlDataReader reader = command.ExecuteReader();

                // Check if player isn't logged in
                bool GotResult = reader.Read();

                reader.Close();
                command.Dispose();
                connection.Close();

                return GotResult;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            return false;
        }
    }
}
