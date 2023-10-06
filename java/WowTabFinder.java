package wowbot;

import com.sun.jna.Native;
import com.sun.jna.win32.W32APIOptions;
import com.sun.jna.platform.win32.WinDef.HWND;

/**
 * This class is used to shift window focus to the WoW program, and fetching current window name.
 * @author Jonas Örnfelt
 *
 */
public class WowTabFinder {

	public interface User32 extends W32APIOptions {
		//Initialize required variables
        User32 instance = (User32) Native.loadLibrary("user32", User32.class, DEFAULT_OPTIONS);
        boolean ShowWindow(HWND hWnd, int nCmdShow);
        boolean SetForegroundWindow(HWND hWnd);
        HWND FindWindow(String winClass, String title);
        HWND GetForegroundWindow();
        int GetWindowTextA(HWND hWnd, byte[] lpString, int nMaxCount);
        int SW_SHOW = 1;
    }
	
	//change focused window to S1
	public static void showWowWindow() {
		User32 user32 = User32.instance;  
        HWND hWnd = user32.FindWindow(null, "World of Warcraft"); // sets focus to wow 
		//HWND hWnd = user32.FindWindow(null, "Hämtade filer"); // sets focus to my opened 'Downloads' folder
        user32.ShowWindow(hWnd, User32.SW_SHOW);  
        user32.SetForegroundWindow(hWnd); 
	}
	
	public static String GetCurrentWindow() {
	    User32 user32 = User32.instance;
	    HWND hwnd = user32.GetForegroundWindow();  // get handle of currently focused window
	    byte[] windowText = new byte[512];  // this size might need to be increased if window titles can be very long
	    user32.GetWindowTextA(hwnd, windowText, 512);
	    return Native.toString(windowText).trim();
	}
}