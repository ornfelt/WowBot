using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms; // For key events and input events
//using System.Data.SqlClient; // If you're using SQL Server
using MySql.Data.MySqlClient;

namespace WowBot
{
    internal class WowBot
    {
        /* Variables needed for Robot */
        private readonly InputManager inputManager;
        private static Random rand = new Random();
        private static DateTimeFormatInfo dtf = new DateTimeFormatInfo { ShortDatePattern = "yy/MM/dd" };
        private static DateTime now;

        // Configuration (mouse clicks not used anymore)
        private MousePos arena2v2;
        private MousePos arena3v3;
        private MousePos arena5v5;
        private MousePos queueJoin;
        private MousePos queueAccept;
        private MousePos bgPress;
        private MousePos bg1;
        private MousePos bg2;
        private MousePos bg3;
        private MousePos bg4;
        private MousePos lowLevelWsg;
        private MousePos acceptRess;

        // Timers
        private const int WSGTIMER = 1800;
        private const int ABTIMER = 1600;
        private const int AVTIMER = 2800;
        private int WsgTurnTimerAlly;
        private int WsgTurnTimerHorde;
        private int AvTurnTimerAlly;
        private int AvTurnTimerHorde;

        // Settings
        private readonly bool isAcore = true; // AzerothCore or TrinityCore
        private readonly bool isLinux = false; // Linux or Windows
        private readonly bool isLocalServer = true; // Connecting to local server or not

        private static bool isArena = false; // Start with BG when random
        private static bool isGroup = false; // If group queue (BG only)
        private static bool otherCTA = false; // If other BG than WSG, AB, AV is call to arms 
        private static bool avCTA = false; // If AV is Call To Arms
        private static bool abCTA = false; // If AB is Call To Arms
        private static bool eyeCTA, strandCTA, isleCTA;
        private static bool isAlly = false; // Faction
        private static int playerLevel = 0; // Player level
        private static int bgCount = 0; // Keep track of how many BGs / arenas that have been played
        private static int bgCountMax = 6; // Max amount of bgCount before switching to BG / arena
        private static string bgInput = "ra"; // Both random BGs and arena
        //private static String bgInput = "r"; // Random BGs
        //private static String bgInput = "a"; // Random arenas
        private const string bgTeleSpotHorde = "silvermooncity";
        private const string bgTeleSpotAlly = "exodar";

        // Horde races
        private static List<int> hordeRaces = new List<int> { 2, 5, 6, 8, 10 };

        internal WowBot(bool isAcore, string nonLocalServerSettings, InputManager inputManager)
        {
            InitSettings();
            rand = new Random();
            this.inputManager = inputManager;
            this.isAcore = isAcore;

            if (nonLocalServerSettings != string.Empty)
            {
                isLocalServer = false;
                var parts = nonLocalServerSettings.Split(';');
                if (parts.Length >= 2)
                {
                    isAlly = parts[0] == "1";

                    try
                    {
                        playerLevel = int.Parse(parts[1]);
                    }
                    catch (FormatException)
                    {
                        Console.Error.WriteLine("Error: The second part of nonLocalServerSettingsArg is not a valid integer.");
                    }
                }
                else
                {
                    Console.Error.WriteLine("Error: nonLocalServerSettingsArg does not contain two values separated by a comma. Please provide isAlly and playerLevel like this: 0,80");
                    Environment.Exit(0);
                }
            }

            Console.WriteLine($"isLinux: {isLinux}, isAcore: {isAcore}, nonLocalServerSettings: {nonLocalServerSettings}");
        }

        private void InitSettings()
        {
            arena2v2 = new MousePos(240, 320);
            arena3v3 = new MousePos(240, 335);
            arena5v5 = new MousePos(240, 350);
            queueJoin = new MousePos(300, 500);
            queueAccept = new MousePos(680, 225);
            bgPress = new MousePos(210, 525);
            bg1 = new MousePos(240, 235);
            bg2 = new MousePos(240, 250);
            bg3 = new MousePos(240, 265);
            bg4 = new MousePos(240, 280);
            lowLevelWsg = new MousePos(240, 220);
            acceptRess = new MousePos(680, 265);
            WsgTurnTimerAlly = 500;
            WsgTurnTimerHorde = 450;
            AvTurnTimerAlly = 130;
            AvTurnTimerHorde = 70;
        }

        private void SetCTA()
        {
            // Calculate current call to arms
            // select * from game_event where holiday in (283, 284, 285, 353, 400, 420);
            // The start dates could be fetched through SQL if needed...
            long occurence = 60480;
            long length = 6240;

            // AV: 283
            avCTA = CheckCTA("2010-05-07 18:00:00", occurence, length);
            Console.WriteLine("AV CTA: " + avCTA);

            // WSG: 284
            bool wsgCTA = CheckCTA("2010-04-02 18:00:00", occurence, length);
            Console.WriteLine("WSG CTA: " + wsgCTA);

            // AB: 285
            abCTA = CheckCTA("2010-04-23 18:00:00", occurence, length);
            Console.WriteLine("AB CTA: " + abCTA);

            // EYE: 353
            eyeCTA = CheckCTA("2010-04-30 18:00:00", occurence, length);
            Console.WriteLine("EYE CTA: " + eyeCTA);

            // Strand: 400
            strandCTA = CheckCTA("2010-04-09 18:00:00", occurence, length);
            Console.WriteLine("Strand CTA: " + strandCTA);

            // Isle: 420
            isleCTA = CheckCTA("2010-04-16 18:00:00", occurence, length);
            Console.WriteLine("Isle CTA: " + isleCTA);

            otherCTA = (eyeCTA || strandCTA || isleCTA);
            Console.WriteLine($"abCTA: {abCTA}, avCTA: {avCTA}, otherCTA: {otherCTA}");
        }

        private bool CheckCTA(string startTime, long occurence, long length)
        {
            // Convert the startTime string to a DateTime object
            DateTime startTimeDate = DateTime.ParseExact(startTime, "yyyy-MM-dd HH:mm:ss", null);
            // Calculate the difference between the current time and the start time
            TimeSpan difference = DateTime.Now - startTimeDate;
            // Convert the difference to seconds
            double differenceInSeconds = difference.TotalSeconds;
            const int MINUTES = 60;
            // Check if the current time is within the occurence and length
            return (differenceInSeconds % (occurence * MINUTES)) < (length * MINUTES);
        }

        void SetPlayerSettings()
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
                string race = "";

                // Ensure player logged in
                if (!reader.Read())
                {
                    Console.WriteLine("Player not logged in. Trying to log in...");
                    inputManager.SendLogin(isAcore, false, false);
                    // Execute SQL again
                    reader.Close();
                    reader = command.ExecuteReader();
                    // Try two more times
                    if (!reader.Read())
                    {
                        Console.WriteLine("Player still not logged in. Trying to log in again...");
                        Thread.Sleep(1000);
                        inputManager.SendLogin(isAcore, true, false);
                        // Execute SQL again
                        reader.Close();
                        reader = command.ExecuteReader();
                        if (!reader.Read())
                        {
                            Console.WriteLine("Player still not logged in. Trying to log in once more...");
                            Thread.Sleep(1000);
                            inputManager.SendLogin(isAcore, false, false);
                            // Execute SQL again
                            reader.Close();
                            reader = command.ExecuteReader();
                            if (!reader.Read())
                                Environment.Exit(0);
                        }
                    }
                }

                race = reader["race"].ToString().Trim();
                playerLevel = Convert.ToInt32(reader["level"]);
                isAlly = !hordeRaces.Contains(Convert.ToInt32(race));
                Console.WriteLine($"\nrace: {race}, level: {playerLevel}, isAlly: {isAlly}");

                reader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        // Start BOT
        public void StartBot()
        {
            while (true)
            {
                // Check game and player status
                Thread.Sleep(3000);
                SetCTA();
                if (isLocalServer)
                    SetPlayerSettings();
                // 5s thread sleep delay
                Thread.Sleep(5000);

                switch (bgInput)
                {
                    case "0":
                        Console.WriteLine($"Starting WSG bot! isAlly: {isAlly}");
                        StartBgBot(0); // WSG
                        break;
                    case "1":
                        Console.WriteLine($"Starting AB bot! isAlly: {isAlly}");
                        StartBgBot(1); // AB
                        break;
                    case "2":
                        Console.WriteLine($"Starting AV bot! isAlly: {isAlly}");
                        StartBgBot(2); // AV
                        break;
                    case "ra":
                        if (bgCount < bgCountMax && isArena)
                        {
                            Console.WriteLine($"Starting arena bot! isAlly: {isAlly}");
                            StartArenaBot(100); // Random arena
                        }
                        else if (bgCount < (bgCountMax / 2) && !isArena)
                        {
                            Console.WriteLine($"Starting random BG bot! isAlly: {isAlly}");
                            StartBgBot(100); // Random BGs
                        }
                        else
                        {
                            // This means bgCountMax has been reached
                            if (isArena)
                            {
                                Console.WriteLine("Switching to playing BGs");
                                Console.WriteLine($"Starting random BG bot! isAlly: {isAlly}");
                                StartBgBot(100); // Random BGs
                            }
                            else
                            {
                                Console.WriteLine("Switching to playing arenas");
                                Console.WriteLine($"Starting arena bot! isAlly: {isAlly}");
                                StartArenaBot(100); // Random arena
                            }
                            bgCount = 0;
                            isArena = !isArena;
                        }
                        bgCount++;
                        break;
                    case "r":
                        Console.WriteLine($"Starting random BG bot! isAlly: {isAlly}");
                        StartBgBot(100); // Random BGs
                        break;
                    case "a":
                    default:
                        Console.WriteLine($"Starting arena bot! isAlly: {isAlly}");
                        //StartArenaBot(0); // 2v2
                        //StartArenaBot(1); // 3v3
                        //StartArenaBot(2); // 5v5
                        StartArenaBot(100); // Random arena
                        break;
                }
            }
        }

        // Start Arena BOT
        void StartArenaBot(int arenaId)
        {
            int timeInBg = 0;
            int maxActionTime = 45;
            int bgTimer = 300;

            Thread.Sleep(1000);
            // Teleport to arena NPC
            inputManager.SendEnter();
            Thread.Sleep(200);
            if (isAlly)
                inputManager.SendKeys(".go creature 68938"); // select guid from creature where id1=19911; (id from arena npc from wowhead)
            else
                inputManager.SendKeys(".go creature 4762"); // select guid from creature where id1=19912; (id from arena npc from wowhead)
            inputManager.SendEnter();

            Thread.Sleep(5000);
            // /target arena char and interact with him
            inputManager.SendEnter();
            Thread.Sleep(200);
            if (isAlly)
                inputManager.SendKeys("/target beka");
            else
                inputManager.SendKeys("/target zeggon");
            inputManager.SendEnter();
            Thread.Sleep(700);
            inputManager.SendKey(Keys.D9);
            Thread.Sleep(1300);

            if (arenaId == 100) // Hard coded, 100 means random arena
                arenaId = new Random().Next(3) + 1;

            if (arenaId == 3) // Extend bgTimer slightly for 5v5
                bgTimer += 50;

            Console.WriteLine($"Playing arena: {arenaId}");
            inputManager.JoinBattlefield(arenaId, isGroup);
            Thread.Sleep(1000);
            inputManager.ClickPopup(); // Accept queue
            Thread.Sleep(5000);

            // Wait for arena to start...
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(9000);
                if (i == 0)
                    inputManager.SendKey(Keys.W, 1000);
                else if (i == 1)
                    inputManager.SendKey(Keys.D, 350);
                else
                {
                    // 20 % chance of jumping, else use spell (scroll down)
                    if (rand.Next(5) == 0)
                        inputManager.SendSpace();
                    else
                    {
                        // Simulate mouse wheel scroll
                        // You might need a different method or library to simulate mouse wheel events in C#
                    }
                }
            }

            // Random spell casting
            for (int i = 0; i < 80 && timeInBg < bgTimer; i++)
            {
                Thread.Sleep(5000); // 5s delay

                // 20 % chance of jumping, else use spell (scroll down)
                if (rand.Next(5) == 0)
                    inputManager.SendSpace();
                else
                {
                    // Simulate mouse wheel scroll
                    // You might need a different method or library to simulate mouse wheel events in C#
                }

                Thread.Sleep(1500); // 1.5s delay
                if (timeInBg < maxActionTime)
                    inputManager.SendKey(Keys.W, 200);
                Thread.Sleep(1500);

                // Use E or 4 spell
                if (timeInBg < maxActionTime)
                {
                    if (rand.Next(2) == 0)
                    {
                        inputManager.SendKeys("t");
                        inputManager.SendKeys("e");
                    }
                    else
                        inputManager.SendKeys("4");
                }

                Thread.Sleep(1500);
                if (timeInBg < maxActionTime)
                {
                    // Use R spell
                    inputManager.SendKeys("r");
                    Thread.Sleep(500);
                    // Use 2
                    inputManager.SendKeys("2");
                    Thread.Sleep(500);
                    // Use shift-w
                    inputManager.SendKeyWithShift(Keys.W);
                }

                timeInBg += 11;
                // Console.WriteLine($"End of loop... timeInBg: {timeInBg}, bgTimer: {bgTimer}");
            }
        }

        // Start Battleground BOT
        void StartBgBot(int bg)
        {
            int timeInBg = 0;
            int bgTimer;

            // Teleport to some place fun
            inputManager.SendEnter();
            Thread.Sleep(100);
            if (isAlly)
                inputManager.SendKeys(".tele " + bgTeleSpotAlly);
            else
                inputManager.SendKeys(".tele " + bgTeleSpotHorde);
            Thread.Sleep(100);
            inputManager.SendEnter();
            Thread.Sleep(5000);

            // Handle random BG
            if (bg == 100) // Hard coded, 100 means random arena
                bg = (playerLevel < 20) ? 0 : (playerLevel < 51) ? rand.Next(2) : rand.Next(3);

            // Set BG timer
            if (bg == 0)
                bgTimer = WSGTIMER;
            else if (bg == 1)
                bgTimer = ABTIMER;
            else
                bgTimer = AVTIMER;

            // Set BG queue index
            int bgQueueIndex;

            // This works 90% of the time
            if (playerLevel < 20)
                bgQueueIndex = 2;
            else if (playerLevel < 51)
                bgQueueIndex = (bg == 0 && !abCTA) || (bg == 1 && abCTA) ? 2 : 3;
            else if (playerLevel < 61)
                bgQueueIndex = bg == 0 ? (!abCTA && !avCTA ? 2 : 3) :
                       bg == 1 ? (abCTA ? 2 : 3) :
                                 (avCTA ? 2 : 4);
            else if (playerLevel < 71)
                bgQueueIndex = bg == 0 ? (!abCTA && !avCTA && !eyeCTA ? 2 : 3) :
                       bg == 1 ? (abCTA ? 2 : (eyeCTA || avCTA ? 4 : 3)) :
                                 (avCTA ? 2 : (eyeCTA ? 5 : 4));
            else
                bgQueueIndex = bg == 0 ? (otherCTA || abCTA || avCTA ? 3 : 2) :
                       bg == 1 ? (otherCTA || avCTA ? 4 : abCTA ? 2 : 3) :
                                 (otherCTA ? 5 : avCTA ? 2 : 4);

            Console.WriteLine($"Queueing for bg: {bg}, bgQueueIndex: {bgQueueIndex}");

            // Join BG
            inputManager.SelectBg(bgQueueIndex);
            inputManager.JoinBattlefield(0, isGroup);
            inputManager.ClickPopup(); // Accept queue
            Thread.Sleep(7000);

            // Wait for BG to start...
            if (bg == 0)
            {
                Thread.Sleep(1000);
                inputManager.SendKey(Keys.D, 500);
                Thread.Sleep(500);
                inputManager.SendKey(Keys.W, 1700);
                Thread.Sleep(1000);

                // Turn slightly in WSG beginning
                inputManager.SendKey(Keys.A, isAlly ? WsgTurnTimerAlly : WsgTurnTimerHorde);

                Thread.Sleep(500);
                inputManager.SendKey(Keys.W, 1500);
                Thread.Sleep(46000);
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(9000);
                    inputManager.SendKey(Keys.W, 1000);
                    Thread.Sleep(100);

                    // Turn slightly in AV beginning
                    if (bg == 2 && i == 0)
                        inputManager.SendKey(Keys.A, 100);
                    else if (bg == 2 && i == 4)
                        inputManager.SendKey(Keys.D, isAlly ? AvTurnTimerAlly : AvTurnTimerHorde);
                }
            }

            // Random walking and some spell casts
            for (int i = 0; i < 100 && timeInBg < bgTimer; i++)
            {
                Thread.Sleep(2000);
                inputManager.SendKey(Keys.W, 200);

                // Auto run
                Thread.Sleep(500);
                inputManager.SendKeyWithAlt(Keys.X);
                Thread.Sleep(9000);
                inputManager.SendKeys("t");
                Thread.Sleep(500);

                // 20 % chance of jumping, else use spell (scroll down)
                if (rand.Next(4) == 0)
                    inputManager.SendKeys("{SPACE}");
                else
                {
                    // Simulate mouse wheel scroll (you might need a different approach or library for this)
                }

                Thread.Sleep(2000);
                inputManager.SendKey(Keys.W, 200);

                // 50 % chance of turning left / right
                if (bg == 0 || timeInBg > 150)
                {
                    if (rand.Next(2) == 0)
                    {
                        //Console.WriteLine("Turning left");
                        inputManager.SendKey(Keys.A, 500);
                        // 50 % chance of turning some more
                        Thread.Sleep(100);
                        if (rand.Next(2) == 0)
                            inputManager.SendKey(Keys.A, 200);
                        else
                            // Else use 2
                            inputManager.SendKeys("2");
                    }
                    else
                    {
                        //Console.WriteLine("Turning right");
                        inputManager.SendKey(Keys.D, 500);
                        Thread.Sleep(100);
                        if (rand.Next(2) == 0)
                            inputManager.SendKey(Keys.D, 200);
                        else
                            // Else use 4
                            inputManager.SendKeys("4");
                    }
                }

                // 30 % chance of clicking release and wait for 30 sec
                if (rand.Next(3) == 0)
                {
                    Thread.Sleep(500);
                    inputManager.ClickPopup();
                    // Wait ~30 sec
                    Thread.Sleep(13000);
                    inputManager.SendKeys("w");
                    Thread.Sleep(15000);
                    timeInBg += 30;
                    // Use shift-w
                    inputManager.SendKeyWithShift(Keys.W);
                }
                timeInBg += 14;

                // Use R spell
                inputManager.SendKeys("r");
                //Console.WriteLine("End of loop... timeInBg: " + timeInBg + ", bgTimer: " + bgTimer);
            }
            if (bg == 2)
                Console.WriteLine($"End of AV loop... timeInBg: {timeInBg}");
        }
    }
}
