using System;
using System.Runtime.InteropServices;

namespace WowBot
{
    internal class WindowFinder
    {
        // Importing the required methods from User32.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        enum ShowWindowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }

        public void ShowWindow(string programName)
        {
            // Find the window handle by window/program title
            IntPtr hWnd = FindWindow(null, programName);

            if (hWnd != IntPtr.Zero)
            {
                // Show and set the foreground window
                ShowWindow(hWnd, ShowWindowCommands.SW_SHOWNORMAL);
                SetForegroundWindow(hWnd);
            }
            else
                Console.WriteLine("Window not found!");
        }
    }
}
