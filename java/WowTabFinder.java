package wowbot;

import com.sun.jna.Native;
import com.sun.jna.win32.W32APIOptions;
import com.sun.jna.platform.win32.WinDef.HWND;
import com.sun.jna.win32.*;

/**
 * This class is used to shift window focus to the "wow" program
 * @author Jonas Örnfelt
 *
 */
public class WowTabFinder {

	public interface User32 extends W32APIOptions {
		//Initialize required variables
        User32 instance = (User32) Native.loadLibrary("user32", User32.class,
                DEFAULT_OPTIONS);
        boolean ShowWindow(HWND hWnd, int nCmdShow);
        boolean SetForegroundWindow(HWND hWnd);
        HWND FindWindow(String winClass, String title);
        int SW_SHOW = 1;
    }
	
	//change focused window to S1
	public void showWowWindow() {
		User32 user32 = User32.instance;  
        HWND hWnd = user32.FindWindow(null, "World of Warcraft"); // sets focus to wow 
		//HWND hWnd = user32.FindWindow(null, "Hämtade filer"); // sets focus to my opened 'Downloads' folder
        user32.ShowWindow(hWnd, User32.SW_SHOW);  
        user32.SetForegroundWindow(hWnd); 
	}
}