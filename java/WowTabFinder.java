package wowbot;

import com.sun.jna.Native;
import com.sun.jna.Pointer;
import com.sun.jna.platform.unix.X11;
import com.sun.jna.platform.unix.X11.Display;
import com.sun.jna.platform.unix.X11.Window;
import com.sun.jna.*;
import com.sun.jna.ptr.*;

import java.io.BufferedReader;
import java.io.InputStreamReader;

// JNA download:
// https://jar-download.com/artifact-search/jna
// JNA platform download:
// https://github.com/java-native-access/jna#download

public class WowTabFinder {
	
	public interface Xlib extends Library {
	    Xlib INSTANCE = (Xlib) Native.load("X11", Xlib.class);
	    int RevertToParent = 2;
	    long CurrentTime = 0;
	    Pointer XOpenDisplay(String display_name);
	    long XRootWindow(Pointer display, int screen_number);
	    int XDefaultScreen(Pointer display);
	    long XInternAtom(Pointer display, String atom_name, boolean only_if_exists);
	    long XGetSelectionOwner(Pointer display, long atom);
	    int XFetchName(Pointer display, long window, Pointer[] window_name_return);
	    int XSetInputFocus(Pointer display, long focus, long revert_to, long time);
	    long XQueryTree(Pointer display, long w, long[] root_return, long[] parent_return, Pointer[] children_return, int[] nchildren_return);
	    int XFree(Pointer data);
	}
    
    public WowTabFinder() {
    }
	
	public static void showWowWindow() {
		// Doesn't work that well with workspaces in X11 / Hyprland
    	//focusWindowByName("World of Warcraft");
		//findWithXdotool();
    }
    
    private static void focusWindowByName(String windowNameToFocus) {
        Xlib xlib = Xlib.INSTANCE;
        Pointer display = xlib.XOpenDisplay(null);
        if (display == null) {
            System.err.println("Unable to open display");
            return;
        }

        long rootWindow = xlib.XRootWindow(display, xlib.XDefaultScreen(display));
        focusWindowByNameRecursive(display, rootWindow, windowNameToFocus);

        xlib.XFree(display);
    }

    private static void focusWindowByNameRecursive(Pointer display, long window, String windowNameToFocus) {
        Xlib xlib = Xlib.INSTANCE;

        Pointer[] children = new Pointer[1];
        int[] childCount = new int[1];
        long[] root = new long[1];
        long[] parent = new long[1];

        xlib.XQueryTree(display, window, root, parent, children, childCount);

        if (childCount[0] > 0) {
            Pointer childArray = children[0];
            for (int i = 0; i < childCount[0]; i++) {
                long childWindow = childArray.getLong(i * Native.POINTER_SIZE);
                Pointer[] windowName = new Pointer[1];
                if (xlib.XFetchName(display, childWindow, windowName) != 0 && windowName[0] != null) {
                    String name = windowName[0].getString(0);
                    if (windowNameToFocus.equals(name)) {
                        xlib.XSetInputFocus(display, childWindow, Xlib.RevertToParent, Xlib.CurrentTime);
                    }
                }
                focusWindowByNameRecursive(display, childWindow, windowNameToFocus);
            }
        }

        xlib.XFree(children[0]);
    }

    private static void findWithXdotool() {
        try {
            // Name or part of the name of the window you want to focus
            String windowName = "World of Warcraft"; 
            
            // Construct the xdotool command to search for the window and activate it
            ProcessBuilder pb = new ProcessBuilder("xdotool", "search", "--name", windowName, "windowactivate", "--sync", "%1");
            
            // Start the process and capture the output
            Process process = pb.start();
            BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
            String line;
            boolean windowFound = false;
            while ((line = reader.readLine()) != null) {
                System.out.println(line);
                windowFound = true;
            }
            
            // If the window was not found, start the program (modify the command as needed to start your program)
            if (!windowFound) {
                ProcessBuilder startPb = new ProcessBuilder("path-to-world-of-warcraft-executable");
                startPb.start();
            }
            
            // Wait for the process to finish and get the exit value
            int exitCode = process.waitFor();
            System.out.println("Process exited with code: " + exitCode);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
    // Get current window name
    public static String GetCurrentWindow() {
		try {
			// Get the ID of the currently focused window
			ProcessBuilder builder = new ProcessBuilder("bash", "-c", "xdotool getwindowfocus");
			Process process = builder.start();
			BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
			String windowId = reader.readLine();
			process.waitFor();

			if (windowId != null && !windowId.trim().isEmpty()) {
				// Get the name of the window using its ID
				builder = new ProcessBuilder("bash", "-c", "xprop -id " + windowId + " | grep 'WM_NAME(STRING)'");
				process = builder.start();
				reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
				String windowName = reader.readLine();
				process.waitFor();

				if (windowName != null) {
					// Extract the window name from the output string
					int startIndex = windowName.indexOf('"') + 1;
					int endIndex = windowName.lastIndexOf('"');
					if (startIndex > 0 && endIndex > startIndex) {
						windowName = windowName.substring(startIndex, endIndex);
						return windowName;
					}
				}
			}
		} catch (Exception e) {
			e.printStackTrace();
		}
		return "";
    }
}