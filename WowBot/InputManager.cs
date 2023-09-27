using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // For key events and input events

namespace WowBot
{
    internal class InputManager
    {
        internal void SendKey(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait(key.ToString());
        }

        internal void SendKeys(string input)
        {
            foreach (char c in input)
            {
                System.Windows.Forms.SendKeys.SendWait(c.ToString());
                Thread.Sleep(200);
            }
        }

        internal void SendKeyWithControl(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("^" + key.ToString());
        }

        internal void SendKeyWithShift(Keys key)
        {
            System.Windows.Forms.SendKeys.SendWait("+" + key.ToString());
        }

        internal void SendTab()
        {
            System.Windows.Forms.SendKeys.SendWait("{TAB}");
        }

        internal void SendEnter()
        {
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        internal void SendSpace()
        {
            System.Windows.Forms.SendKeys.SendWait(" ");
        }

        internal void SendEscape()
        {
            System.Windows.Forms.SendKeys.SendWait("{CAPSLOCK}");
        }

        internal void SendLogin(bool isAcore, bool isDC)
        {
            Thread.Sleep(3000);
            // Press enter to get rid of DC message
            if (isDC)
                SendEnter();
            Thread.Sleep(1000);
            // Ctrl-a to mark all text 
            SendKeyWithControl(Keys.A);
            Thread.Sleep(500);
            SendKeys(isAcore ? "acore2" : "tcore2");
            Thread.Sleep(200);
            SendTab();
            Thread.Sleep(200);
            SendKeys("123");
            Thread.Sleep(200);
            SendEnter();
            Thread.Sleep(9000);
            SendEnter();
            Thread.Sleep(9000);
            SendEnter();
            Thread.Sleep(300);
            SendEnter();
            Thread.Sleep(6000);
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    }
}
