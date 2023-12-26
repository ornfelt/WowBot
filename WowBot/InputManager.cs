using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // For key events and input events

namespace WowBot
{
    internal class InputManager
    {

        private readonly Dictionary<int, string> battlegroundNames = new Dictionary<int, string>()
        {
            { 0, "Warsong Gulch" },
            { 1, "Arathi Basin" },
            { 2, "Alterac Valley" }
        };

        internal void JoinBattlefield(int index, bool isGroup)
        {
            //if (!WindowFinder.GetCurrentWindow().Contains(wowName))
            //    return;

            SendEnter();
            SendKeys($"/run JoinBattlefield({index},{(isGroup ? "1" : "0")})");
            SendEnter();
        }

        internal void TogglePVPFrame()
        {
            //if (!WindowFinder.GetCurrentWindow().Contains(wowName))
            //    return;

            SendEnter();
            SendKeys("/run TogglePVPFrame()");
            SendEnter();
        }

        internal void SelectBg(int index)
        {
            //if (!WindowFinder.GetCurrentWindow().Contains(wowName))
            //    return;

            TogglePVPFrame();
            SendEnter();
            SendKeys("/click PVPParentFrameTab2");
            SendEnter();
            Thread.Sleep(300);
            SendEnter();
            //SendKeys($"/run PVPBattlegroundFrame.selectedBG = {index}");
			// Join through Lua instead
			String luaScript = "/run for i=1,GetNumBattlegroundTypes() do " +
                   "local name, x = GetBattlegroundInfo(i) " +
                   "if name == '" + battlegroundNames[index] + "' then " +
                   //"print(name .. x) " +
                   "PVPBattlegroundFrame.selectedBG = i " +
                   "end " +
                   "end";
			SendKeys(luaScript);
            SendEnter();
            Thread.Sleep(300);
            TogglePVPFrame();
            Thread.Sleep(300);
            TogglePVPFrame();
            Thread.Sleep(300);
        }

        internal void ClickPopup()
        {
            //if (!WindowFinder.GetCurrentWindow().Contains(wowName))
            //    return;

            SendEnter();
            SendKeys("/click StaticPopup1Button1");
            SendEnter();
        }

        internal void SendKey(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait(key.ToString().ToLower());
        }

        internal void SendKey(Keys key, int durationMilliseconds)
        {
            keybd_event((byte)key, 0, 0, IntPtr.Zero); // Key down
            Thread.Sleep(durationMilliseconds);
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, IntPtr.Zero); // Key up
        }

        internal void SendKeys(string input)
        {
            foreach (char c in input)
            {
                string keyString = c.ToString();
                switch (keyString)
                {
                    case "(":
                        keyString = "{(}";
                        break;
                    case ")":
                        keyString = "{)}";
                        break;
                    default:
                        break;
                }
                System.Windows.Forms.SendKeys.SendWait(keyString);
                Thread.Sleep(200);
            }
        }

        internal void SendKeyWithControl(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("^" + key.ToString().ToLower());
        }

        internal void SendKeyWithShift(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("+" + key.ToString().ToLower());
        }

        internal void SendKeyWithAlt(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("%" + key.ToString().ToLower());
        }

        internal void SendTab()
        {
            System.Windows.Forms.SendKeys.SendWait("{TAB}");
        }

        internal void SendEnter()
        {
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            Thread.Sleep(200);
        }

        internal void SendEnterInWow()
        {
            if (!WindowFinder.GetCurrentWindow().Contains("World of Warcraft"))
                return;
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            Thread.Sleep(200);
        }

        internal void SendSpace()
        {
            System.Windows.Forms.SendKeys.SendWait(" ");
        }

        internal void SendEscape()
        {
            System.Windows.Forms.SendKeys.SendWait("{CAPSLOCK}");
        }

        internal void SendLogin(bool isAcore, bool isDC, bool isBloogBot)
        {
            Thread.Sleep(2000);
            // Press enter to get rid of DC message
            if (isDC)
                SendEnter();
            Thread.Sleep(1000);
            // Ctrl-a to mark all text 
            SendKeyWithControl(Keys.A);
            Thread.Sleep(500);
            SendKeys(isAcore ? (isBloogBot ? "acore2" : "acore") : (isBloogBot ? "tcore2" : "tcore"));
            Thread.Sleep(200);
            SendTab();
            Thread.Sleep(200);
            SendKeys("123");
            Thread.Sleep(200);
            SendEnter();
            Thread.Sleep(5000);
            SendEnter();
            Thread.Sleep(8000);
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            Thread.Sleep(50);
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            Thread.Sleep(10000);
        }

        internal void MouseClick()
        {
            Thread.Sleep(500);
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

        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    }
}
