using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms; // For key events and input events
using System.Drawing; // For Robot-like functionality, though C# doesn't have a direct equivalent
using System.Windows; // For Clipboard
using System.Data; // For SQL related operations
//using System.Data.SqlClient; // If you're using SQL Server
using MySql.Data.MySqlClient;

namespace WowBot
{
    internal class WowBot
    {
        internal class MousePos
        {
            public readonly int X;
            public readonly int Y;

            public MousePos(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        /* Variables needed for Robot */
        private static Random rand = new Random();
        DateTimeFormatInfo dtf = new DateTimeFormatInfo { ShortDatePattern = "yy/MM/dd" };
        DateTime now;

        // Configuration
        private MousePos arena2v2 = new MousePos(240, 408);
        private MousePos arena3v3 = new MousePos(240, 420);
        private MousePos arena5v5 = new MousePos(240, 440);
        private MousePos queueJoin = new MousePos(290, 662);
        private MousePos queueAccept = new MousePos(850, 265);
        private MousePos bgPress = new MousePos(180, 695);
        private MousePos bg1 = new MousePos(240, 290);
        private MousePos bg2 = new MousePos(240, 308);
        private MousePos bg3 = new MousePos(240, 330);
        private MousePos bg4 = new MousePos(240, 340);
        private MousePos lowLevelWsg = new MousePos(240, 270);
        private MousePos acceptRess = new MousePos(900, 265);

        // Timers
        private const int WSGTIMER = 1900;
        private const int ABTIMER = 1600;
        private const int AVTIMER = 2700;
        private const int WSGTURNTIMERALLY = 500;
        private const int WSGTURNTIMERHORDE = 450;
        private const int AVTURNTIMERALLY = 130;
        private const int AVTURNTIMERHORDE = 70;

        // Queue settings
        private static bool isAcore = true; // AzerothCore / TrinityCore
        private static bool isArena = false; // Start with BG when random
        private static bool isGroup = false; // If group queue (BG only)
        private static bool isLowLevel = false; // If low level (special ordering of BGs)
        private static bool otherCTA = false; // If other BG than WSG, AB, AV is call to arms 
        private static bool avCTA = false; // If AV is Call To Arms
        private static bool abCTA = false; // If AB is Call To Arms
        private static bool isAlly = false; // Faction
        private static int bgCount = 0; // Keep track of how many BGs / arenas that have been played
        private static int bgCountMax = 6; // Max amount of bgCount before switching to BG / arena
        private static string bgInput = "ra"; // Both random BGs and arena
        private const string bgTeleSpotHorde = "silvermooncity";
        private const string bgTeleSpotAlly = "exodar";

        // Horde races
        private static List<int> hordeRaces = new List<int> { 2, 5, 6, 8, 10 };
        // The order of the BGs might change depending on current Call to Arms
        private Dictionary<int, int> bgOrderMap;

        internal WowBot()
        {
            rand = new Random();
        }

        void SetCTA()
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
            bool eyeCTA = CheckCTA("2010-04-30 18:00:00", occurence, length);
            Console.WriteLine("EYE CTA: " + eyeCTA);

            // Strand: 400
            bool strandCTA = CheckCTA("2010-04-09 18:00:00", occurence, length);
            Console.WriteLine("Strand CTA: " + strandCTA);

            // Isle: 420
            bool isleCTA = CheckCTA("2010-04-16 18:00:00", occurence, length);
            Console.WriteLine("Isle CTA: " + isleCTA);

            otherCTA = (eyeCTA || strandCTA || isleCTA);
            Console.WriteLine($"abCTA: {abCTA}, avCTA: {avCTA}, otherCTA: {otherCTA}");

            bgOrderMap = new Dictionary<int, int>();
            if (otherCTA)
            {
                bgOrderMap[0] = 2; // WSG 2
                bgOrderMap[1] = 3; // AB 3
                bgOrderMap[2] = 4; // AV 4
            }
            else if (avCTA)
            {
                bgOrderMap[2] = 1; // AV 1
                bgOrderMap[0] = 2; // WSG 2
                bgOrderMap[1] = 3; // AB 3
            }
            else if (abCTA)
            {
                bgOrderMap[1] = 1; // AB 1
                bgOrderMap[0] = 2; // WSG 2
                bgOrderMap[2] = 3; // AV 3
            }
            else
            {
                bgOrderMap[0] = 1; // WSG 1
                bgOrderMap[1] = 2; // AB 2
                bgOrderMap[2] = 3; // AV 3
            }
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

        // Try to login
        void TryLogin()
        {
            Thread.Sleep(2000);
            // Press enter to get rid of DC message
            SendEnter();
            Thread.Sleep(1000);
            // Ctrl-a to mark all text 
            SendKeyWithControl(Keys.A);
            Thread.Sleep(500);
            SendKeys(isAcore ? "acore" : "tcore");
            Thread.Sleep(200);
            SendTab();
            Thread.Sleep(200);
            SendKeys("123");
            Thread.Sleep(200);
            SendEnter();
            Thread.Sleep(5000);
            SendEnter();
            Thread.Sleep(5000);
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
                    connection = new MySqlConnection( "Server=localhost;Database=acore_characters;User ID=acore;Password=acore;");
                else
                    connection = new MySqlConnection( "Server=localhost;Database=characters;User ID=trinity;Password=trinity;");
                connection.Open();

                int accountId = 1;
                //MySqlCommand command = new MySqlCommand($"select name, race, level from characters where online = 1 and account = {accountId}", connection);
                MySqlCommand command = new MySqlCommand($"select name, race, level from characters where online = 1", connection);
                MySqlDataReader reader = command.ExecuteReader();

                string race = "";
                int level = 0;

                // Check if player isn't logged in
                if (!reader.Read())
                {
                    Console.WriteLine("Player not logged in. Trying to log in...");
                    TryLogin();
                    // Execute SQL again
                    reader.Close();
                    reader = command.ExecuteReader();
                    // Try one more time
                    if (!reader.Read())
                    {
                        Console.WriteLine("Player still not logged in. Trying to log in once more...");
                        Thread.Sleep(1000);
                        SendEnter();
                        TryLogin();
                        // Execute SQL again
                        reader.Close();
                        reader = command.ExecuteReader();
                        if (!reader.Read())
                            Environment.Exit(0);
                    }
                }

                race = reader["race"].ToString().Trim();
                level = Convert.ToInt32(reader["level"]);
                isAlly = !hordeRaces.Contains(Convert.ToInt32(race));
                isLowLevel = level < 70;
                Console.WriteLine($"\nrace: {race}, level: {level}");
                Console.WriteLine($"isAlly: {isAlly}, isLowLevel: {isLowLevel}");

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
                SetPlayerSettings();
                // 5s thread sleep delay
                Thread.Sleep(5000);

                switch (bgInput)
                {
                    case "0":
                        Console.WriteLine($"Starting WSG bot! isAlly: {isAlly}");
                        StartBgBot(0, WSGTIMER, isAlly, isLowLevel); // WSG
                        break;
                    case "1":
                        Console.WriteLine($"Starting AB bot! isAlly: {isAlly}");
                        StartBgBot(1, ABTIMER, isAlly, isLowLevel); // AB
                        break;
                    case "2":
                        Console.WriteLine($"Starting AV bot! isAlly: {isAlly}");
                        StartBgBot(2, AVTIMER, isAlly, isLowLevel); // AV
                        break;
                    case "ra":
                        if (bgCount < bgCountMax && isArena)
                        {
                            Console.WriteLine($"Starting arena bot! isAlly: {isAlly}");
                            StartArenaBot(100, 250, isAlly); // Random arena
                        }
                        else if (bgCount < (bgCountMax / 2) && !isArena)
                        {
                            Console.WriteLine($"Starting random BG bot! isAlly: {isAlly}");
                            StartBgBot(100, 0, isAlly, isLowLevel); // Random BGs
                        }
                        else
                        {
                            // This means bgCountMax has been reached
                            if (isArena)
                            {
                                Console.WriteLine("Switching to playing BGs");
                                Console.WriteLine($"Starting random BG bot! isAlly: {isAlly}");
                                StartBgBot(100, 0, isAlly, isLowLevel); // Random BGs
                            }
                            else
                            {
                                Console.WriteLine("Switching to playing arenas");
                                Console.WriteLine($"Starting arena bot! isAlly: {isAlly}");
                                StartArenaBot(100, 250, isAlly); // Random arena
                            }
                            bgCount = 0;
                            isArena = !isArena;
                        }
                        bgCount++;
                        break;
                    case "r":
                        Console.WriteLine($"Starting random BG bot! isAlly: {isAlly}");
                        StartBgBot(100, 0, isAlly, isLowLevel); // Random BGs
                        break;
                    case "a":
                    default:
                        Console.WriteLine($"Starting arena bot! isAlly: {isAlly}");
                        //StartArenaBot(0, 250, isAlly); // 2v2
                        //StartArenaBot(1, 250, isAlly); // 3v3
                        //StartArenaBot(2, 250, isAlly); // 5v5
                        StartArenaBot(100, 250, isAlly); // Random arena
                        break;
                }
            }
        }

        // Start Arena BOT
        void StartArenaBot(int arenaId, int bgTimer, bool isAlly)
        {
            int timeInBg = 0;
            int maxActionTime = 45;
            Thread.Sleep(1000);
            // Teleport to arena NPC
            SendEnter();
            Thread.Sleep(200);
            if (isAlly)
                SendKeys(".go creature 68938"); // select guid from creature where id1=19911; (id from arena npc from wowhead)
            else
                SendKeys(".go creature 4762"); // select guid from creature where id1=19912; (id from arena npc from wowhead)
            SendEnter();

            Thread.Sleep(5000);
            // /target arena char and interact with him
            SendEnter();
            // Enter '/' manually for Linux
            Thread.Sleep(200);
            SendKeyWithShift(Keys.D7);
            if (isAlly)
                SendKeys("target beka");
            else
                SendKeys("target zeggon");
            SendEnter();
            Thread.Sleep(700);
            SendKey(Keys.D9);
            Thread.Sleep(1300);

            if (arenaId == 100) // Hard coded, 100 means random arena
                arenaId = new Random().Next(3);

            Console.WriteLine($"Playing arena: {arenaId}");

            if (arenaId == 2) // Extend bgTimer slightly for 5v5
                bgTimer += 50;

            Point targetPoint;
            if (arenaId == 0)
                targetPoint = new Point(arena2v2.X, arena2v2.Y); // 2v2
            else if (arenaId == 1)
                targetPoint = new Point(arena3v3.X, arena3v3.Y); // 3v3
            else
                targetPoint = new Point(arena5v5.X, arena5v5.Y); // 5v5

            Cursor.Position = targetPoint;
            Thread.Sleep(700);
            MouseClick();

            Thread.Sleep(1000);
            Cursor.Position = new Point(queueJoin.X, queueJoin.Y); // Join queue
            Thread.Sleep(500);
            MouseClick();

            Thread.Sleep(3000);
            Cursor.Position = new Point(queueAccept.X, queueAccept.Y); // Accept queue inv
            Thread.Sleep(500);
            MouseClick();

            Thread.Sleep(5000);
            MouseClick();

            // Wait for arena to start...
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(9000);
                if (i == 0)
                {
                    SendKeys("w");
                    Thread.Sleep(1000);
                }
                else if (i == 1)
                {
                    SendKeys("d");
                    Thread.Sleep(350);
                }
                else
                {
                    // 20 % chance of jumping, else use spell (scroll down)
                    if (rand.Next(5) == 0)
                    {
                        System.Windows.Forms.SendKeys.SendWait("{SPACE}");
                        Thread.Sleep(100);
                    }
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
                {
                    System.Windows.Forms.SendKeys.SendWait("{SPACE}");
                    Thread.Sleep(200);
                }
                else
                {
                    // Simulate mouse wheel scroll
                    // You might need a different method or library to simulate mouse wheel events in C#
                }

                Thread.Sleep(1500); // 1.5s delay

                if (timeInBg < maxActionTime)
                {
                    SendKeys("w");
                    Thread.Sleep(200);
                }

                Thread.Sleep(1500);

                // Use E or 4 spell
                if (timeInBg < maxActionTime)
                {
                    if (rand.Next(2) == 0)
                    {
                        SendKeys("t");
                        Thread.Sleep(600);
                        SendKeys("e");
                    }
                    else
                    {
                        SendKeys("4");
                        Thread.Sleep(400);
                    }
                }

                Thread.Sleep(1000);

                if (timeInBg < maxActionTime)
                {
                    // Use R spell
                    SendKeys("r");
                    Thread.Sleep(200);

                    // Use 2
                    Thread.Sleep(400);
                    SendKeys("2");
                    Thread.Sleep(580);

                    // Use shift-w
                    SendKeyWithShift(Keys.W);
                    Thread.Sleep(140);
                }

                timeInBg += 11;
                // Console.WriteLine($"End of loop... timeInBg: {timeInBg}, bgTimer: {bgTimer}");
            }
        }

        // Start Battleground BOT
        void StartBgBot(int bg, int bgTimer, bool isAlly, bool isLowLevel)
        {
            int timeInBg = 0;

            // Teleport to some place fun
            SendEnter();
            Thread.Sleep(100);
            if (isAlly)
                SendKeys(".tele " + bgTeleSpotAlly);
            else
                SendKeys(".tele " + bgTeleSpotHorde);
            Thread.Sleep(100);
            SendEnter();

            Thread.Sleep(5000);
            // Open PVP window
            SendKey(Keys.H);
            Thread.Sleep(1000);
            Cursor.Position = new System.Drawing.Point(bgPress.X, bgPress.Y); // Press Battlegrounds
            Thread.Sleep(500);
            MouseClick();

            // Handle random BG
            if (bg == 100) // Hard coded, 100 means random arena
                bg = new Random().Next(3);
            Console.WriteLine($"Playing BG: {bg}");
            // Set correct bgTimer
            if (bg == 0)
                bgTimer = WSGTIMER;
            else if (bg == 1)
                bgTimer = ABTIMER;
            else
                bgTimer = AVTIMER;

            Thread.Sleep(1000);
            if (bg == 0)
            {
                switch ((int)bgOrderMap[bg])
                {
                    case 1:
                        Cursor.Position = new System.Drawing.Point(bg1.X, bg1.Y); // WSG 1
                        break;
                    case 2:
                    default:
                        Cursor.Position = new System.Drawing.Point(bg2.X, bg2.Y); // WSG 2
                        break;
                }
            }
            else if (bg == 1)
            {
                switch ((int)bgOrderMap[bg])
                {
                    case 1:
                        Cursor.Position = new System.Drawing.Point(bg1.X, bg1.Y); // AB 1
                        break;
                    case 2:
                        Cursor.Position = new System.Drawing.Point(bg2.X, bg2.Y); // AB 2
                        break;
                    case 3:
                    default:
                        Cursor.Position = new System.Drawing.Point(bg3.X, bg3.Y); // AB 3
                        break;
                }
            }
            else
            {
                switch ((int)bgOrderMap[bg])
                {
                    case 1:
                        Cursor.Position = new System.Drawing.Point(bg1.X, bg1.Y); // AV 1
                        break;
                    case 3:
                        Cursor.Position = new System.Drawing.Point(bg3.X, bg3.Y); // AV 3
                        break;
                    case 4:
                    default:
                        Cursor.Position = new System.Drawing.Point(bg4.X, bg4.Y); // AV 4
                        break;
                }
            }

            // USE THIS IF LOW LEVEL
            if (isLowLevel)
            {
                if (otherCTA)
                {
                    if (bg == 0)
                        Cursor.Position = new System.Drawing.Point(bg1.X, bg1.Y); // WSG
                    else if (bg == 1)
                        Cursor.Position = new System.Drawing.Point(bg2.X, bg2.Y); // AB
                    else
                        Cursor.Position = new System.Drawing.Point(bg3.X, bg3.Y); // AV
                }
                else
                {
                    if (bg == 0)
                        Cursor.Position = new System.Drawing.Point(lowLevelWsg.X, lowLevelWsg.Y); // WSG
                    else if (bg == 1)
                        Cursor.Position = new System.Drawing.Point(bg1.X, bg1.Y); // AB
                    else
                        Cursor.Position = new System.Drawing.Point(bg2.X, bg2.Y); // AV
                }
            }

            // Click
            Thread.Sleep(500);
            MouseClick();

            Thread.Sleep(1000);
            if (isGroup)
                Cursor.Position = new System.Drawing.Point(queueJoin.X - 120, queueJoin.Y); // Join group queue
            else
                Cursor.Position = new System.Drawing.Point(queueJoin.X, queueJoin.Y); // Join queue

            // Click
            Thread.Sleep(500);
            MouseClick();

            Thread.Sleep(3000);
            Cursor.Position = new System.Drawing.Point(queueAccept.X, queueAccept.Y); // Accept queue inv

            // Click
            Thread.Sleep(500);
            MouseClick();

            Thread.Sleep(5000);
            // Click
            MouseClick();

            // Wait for BG to start...
            if (bg == 0)
            {
                Thread.Sleep(1000);
                SendKeys("d");
                Thread.Sleep(500);
                SendKeys("d"); // Release the 'd' key
                Thread.Sleep(500);
                SendKeys("w");
                Thread.Sleep(1700);
                SendKeys("w"); // Release the 'w' key
                Thread.Sleep(1000);
                SendKeys("a");

                // Turn slightly in WSG beginning
                if (isAlly)
                    Thread.Sleep(WSGTURNTIMERALLY); // Ally
                else
                    Thread.Sleep(WSGTURNTIMERHORDE); // Horde

                SendKeys("a"); // Release the 'a' key
                Thread.Sleep(500);
                SendKeys("w");
                Thread.Sleep(1500);
                SendKeys("w"); // Release the 'w' key
                Thread.Sleep(46000);
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(9000);
                    SendKeys("w");
                    Thread.Sleep(1000);
                    SendKeys("w"); // Release the 'w' key

                    // Turn slightly in AV beginning
                    if (bg == 2 && i == 0)
                    {
                        Thread.Sleep(100);
                        SendKeys("a");
                        Thread.Sleep(100);
                        SendKeys("a"); // Release the 'a' key
                    }
                    else if (bg == 2 && i == 4)
                    {
                        Thread.Sleep(100);
                        SendKeys("d");
                        if (isAlly)
                            Thread.Sleep(AVTURNTIMERALLY);
                        else
                            Thread.Sleep(AVTURNTIMERHORDE);
                        SendKeys("d"); // Release the 'd' key
                    }
                }
            }

            // Random walking and some spell casts
            for (int i = 0; i < 100 && timeInBg < bgTimer; i++)
            {
                Thread.Sleep(2000);
                SendKeys("w");
                Thread.Sleep(100);
                SendKeys("w"); // Release the 'w' key
                Thread.Sleep(100);

                // Auto run
                Thread.Sleep(500);
                SendKeys("%x"); // % represents the ALT key
                Thread.Sleep(9000);
                SendKeys("t");
                Thread.Sleep(500);

                // 20 % chance of jumping, else use spell (scroll down)
                if (rand.Next(4) == 0)
                {
                    SendKeys("{SPACE}");
                    Thread.Sleep(500);
                }
                else
                {
                    // Simulate mouse wheel scroll (you might need a different approach or library for this)
                }

                Thread.Sleep(1500);
                SendKeys("w");
                Thread.Sleep(200);

                // 50 % chance of turning left / right
                if (bg == 0 || timeInBg > 150)
                {
                    if (rand.Next(2) == 0)
                    {
                        SendKeys("a");
                        Thread.Sleep(500);
                        if (rand.Next(2) == 0)
                        {
                            Thread.Sleep(100);
                            SendKeys("a");
                            Thread.Sleep(300);
                        }
                        else
                        {
                            Thread.Sleep(100);
                            SendKeys("2");
                        }
                    }
                    else
                    {
                        SendKeys("d");
                        Thread.Sleep(500);
                        if (rand.Next(2) == 0)
                        {
                            Thread.Sleep(100);
                            SendKeys("d");
                            Thread.Sleep(300);
                        }
                        else
                        {
                            Thread.Sleep(100);
                            SendKeys("4");
                        }
                    }
                }

                // 30 % chance of clicking release and wait for 30 sec
                if (rand.Next(3) == 0)
                {
                    // First try to accept ress from someone, then try to release
                    Thread.Sleep(500);
                    Cursor.Position = new System.Drawing.Point(queueAccept.X, queueAccept.Y);
                    Thread.Sleep(500);
                    MouseClick(); // Click
                    Thread.Sleep(500);
                    Cursor.Position = new System.Drawing.Point(acceptRess.X, acceptRess.Y);
                    Thread.Sleep(500);
                    MouseClick(); // Click
                    // Try clicking a bit further down as well since
                    // release button can be moved down if bot 
                    // ressed player but it expired before getting accepted
                    Thread.Sleep(500);
                    Cursor.Position = new System.Drawing.Point(acceptRess.X, acceptRess.Y+50);
                    Thread.Sleep(500);
                    MouseClick(); // Click

                    // Wait 30 sec
                    Thread.Sleep(12000);
                    SendKeys("w");
                    Thread.Sleep(150);
                    Thread.Sleep(14500);
                    timeInBg += 30;

                    // Also use shift-w
                    SendKeys("+w"); // + represents the SHIFT key
                    Thread.Sleep(140);
                }
                timeInBg += 14;

                // Use R spell
                SendKeys("r");
                Thread.Sleep(200);
            }
            if (bg == 2)
                Console.WriteLine($"End of AV loop... timeInBg: {timeInBg}");
        }

        void SendTab()
        {
            System.Windows.Forms.SendKeys.SendWait("{TAB}");
        }

        void SendEnter()
        {
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        void SendKey(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait(key.ToString());
        }

        void SendKeys(string input)
        {
            foreach (char c in input)
            {
                System.Windows.Forms.SendKeys.SendWait(c.ToString());
                Thread.Sleep(200);
            }
        }

        void SendKeyWithControl(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("^" + key.ToString());
        }

        void SendKeyWithShift(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("+" + key.ToString());
        }

        void MouseClick()
        {
            MouseButtons button = MouseButtons.Left;
            int x = Cursor.Position.X;
            int y = Cursor.Position.Y;
            mouse_event((int)(MouseEventFlags.LEFTDOWN), x, y, 0, 0);
            mouse_event((int)(MouseEventFlags.LEFTUP), x, y, 0, 0);
        }

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    }
}
