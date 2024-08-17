package wowbot;

import java.awt.AWTException;
import java.awt.Robot;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.util.HashMap;
import java.util.Map;

public class InputManager {
	
	private Robot r;
	private final static String wowName = "World of Warcraft";
	private boolean isLinux = false;
	private static Map<Integer, String> battlegroundNames = new HashMap<>();
    static {
        battlegroundNames.put(0, "Warsong Gulch");
        battlegroundNames.put(1, "Arathi Basin");
        battlegroundNames.put(2, "Alterac Valley");
    }
	
	public InputManager(Robot robot, boolean isLinux) {
		r = robot;
		this.isLinux = isLinux;
	}
	
	void joinBattlefield(int index, boolean isGroup) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		sendKey(KeyEvent.VK_ENTER);
		sendKeys("/run JoinBattlefield(" + index + "," + (isGroup ? "1" : "0") + ")");
		sendKey(KeyEvent.VK_ENTER);
	}

	void togglePVPFrame() {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		sendKey(KeyEvent.VK_ENTER);
		sendKeys("/run TogglePVPFrame()");
		sendKey(KeyEvent.VK_ENTER);
	}
	
	// Use /framestack in-game to find names for buttons / frames
	void selectBg(int index) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		togglePVPFrame();
		sendKey(KeyEvent.VK_ENTER);
		sendKeys("/click PVPParentFrameTab2");
		sendKey(KeyEvent.VK_ENTER);
		r.delay(300);
		sendKey(KeyEvent.VK_ENTER);
		//sendKeys("/run PVPBattlegroundFrame.selectedBG = " + index);
		// Join through Lua instead
		String luaScript = "/run for i=1,GetNumBattlegroundTypes() do " +
                   "local name, x = GetBattlegroundInfo(i) " +
                   "if name == '" + battlegroundNames.get(index) + "' then " +
                   //"print(name .. x) " +
                   "PVPBattlegroundFrame.selectedBG = i " +
                   "end " +
                   "end";
		sendKeys(luaScript);
		sendKey(KeyEvent.VK_ENTER);
		r.delay(300);
		togglePVPFrame();
		r.delay(300);
		togglePVPFrame();
		r.delay(300);
	}
	
	void clickPopup() {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		sendKey(KeyEvent.VK_ENTER);
		sendKeys("/click StaticPopup1Button1");
		sendKey(KeyEvent.VK_ENTER);
	}

	// Execute specific key
	void sendKey(int key) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.keyPress(key);
		r.delay(200);
		r.keyRelease(key);
		r.delay(200);
	}

	void sendKey(int key, int delay) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.keyPress(key);
		r.delay(delay);
		r.keyRelease(key);
		r.delay(60);
	}

	void sendKeyWithShift(int key) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.keyPress(KeyEvent.VK_SHIFT);
		r.delay(30);
		r.keyPress(key);
		r.delay(30);
		r.keyRelease(KeyEvent.VK_SHIFT);
		r.delay(30);
		r.keyRelease(key);
		r.delay(30);
	}

	void sendKeyWithCtrl(int key) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.keyPress(KeyEvent.VK_CONTROL);
		r.delay(30);
		r.keyPress(key);
		r.delay(30);
		r.keyRelease(KeyEvent.VK_CONTROL);
		r.delay(30);
		r.keyRelease(key);
		r.delay(30);
	}

	void sendKeyWithAlt(int key) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.keyPress(KeyEvent.VK_ALT);
		r.delay(30);
		r.keyPress(key);
		r.delay(30);
		r.keyRelease(KeyEvent.VK_ALT);
		r.delay(30);
		r.keyRelease(key);
		r.delay(30);
	}
	
	// Execute the characters in string key 
	void sendKeys(String keys) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
	    for (char c : keys.toCharArray()) {
	    	if(c == 'Å') {
	    		keyPress('Å');
	    	}else if (c == 'Ä') {
	    		keyPress('Ä');
	    	}else if (c == 'Ö') {
	    		keyPress('Ö');
	    	}else if (c == 'å') {
	    		keyPress('å');
	    	}else if (c == 'ä') {
	    		keyPress('ä');
	    	}else if (c == 'ö') {
	    		keyPress('ö');
	    	}else if (c == '&') {
	    		keyPress('&');
	    	}else if (c == '#') {
	    		keyPress('#');
	    	}else if (c == '!') {
	    		keyPress('!');
	    	}else if (c == '/') {
	    		if (!isLinux)
					keyPress('/');
	    		else
					sendKeyWithShift(KeyEvent.VK_7);
	    	}else if (c == '(' && isLinux) {
				sendKeyWithShift(KeyEvent.VK_8);
	    	}else if (c == ')' && isLinux) {
				sendKeyWithShift(KeyEvent.VK_9);
	    	}else if (c == '=' && isLinux) {
				sendKeyWithShift(KeyEvent.VK_0);
	    	}else if (c == ':') {
	    		keyPress(':');
	    	}else if (c == '@') {
	    		keyPress('@');
	    	} else if (Character.isUpperCase(c)) {
				int keyCode = KeyEvent.getExtendedKeyCodeForChar(c);
				sendKeyWithShift(keyCode);
	    	}else {
				int keyCode = KeyEvent.getExtendedKeyCodeForChar(c);
				if (KeyEvent.CHAR_UNDEFINED == keyCode) {
					throw new RuntimeException(
						"Key code not found for character '" + c + "'");
				}
				r.delay(100);
				r.keyPress(keyCode);
				r.delay(50);
				r.keyRelease(keyCode);
				r.delay(100);
	    	}
	    }
	}
	
	// Press keys via altNumpad
	public void keyPress(char characterKey){
	    switch (characterKey){
	        case '!': altNumpad("33"); break;
	        case '"': altNumpad("34"); break;
	        case '#': altNumpad("35"); break;
	        case '$': altNumpad("36"); break;
	        case '%': altNumpad("37"); break;
	        case '&': altNumpad("38"); break;
	        case '\'': altNumpad("39"); break;
	        case '(': altNumpad("40"); break;
	        case ')': altNumpad("41"); break;
	        case '*': altNumpad("42"); break;
	        case '+': altNumpad("43"); break;
	        case ',': altNumpad("44"); break;
	        case '-': altNumpad("45"); break;
	        case '.': altNumpad("46"); break;
	        case '/': altNumpad("47"); break;
	        case '0': altNumpad("48"); break;
	        case ':': altNumpad("58"); break;
	        case '@': altNumpad("64"); break;
	        case 'å': altNumpad("134"); break;
	        case 'ä': altNumpad("132"); break;
	        case 'ö': altNumpad("148"); break;
	        case 'Å': altNumpad("143"); break;
	        case 'Ä': altNumpad("142"); break;
	        case 'Ö': altNumpad("153"); break;
	        default: return;
	    }
	}

	// altNumpad for special characters
	private void altNumpad(int... numpadCodes){
	    if (numpadCodes.length == 0) {
	        return;
	    }
	    r.keyPress(KeyEvent.VK_ALT);

	    for (int NUMPAD_KEY : numpadCodes){
	        r.keyPress(NUMPAD_KEY);
	        r.keyRelease(NUMPAD_KEY);
	    }
	    r.keyRelease(KeyEvent.VK_ALT);
	}

	// altNumpad for special characters
	private void altNumpad(String numpadCodes){
	    if (numpadCodes == null || !numpadCodes.matches("^\\d+$")){
	        return;
	    }               
	    r.keyPress(KeyEvent.VK_ALT);
		
	    for (char charater : numpadCodes.toCharArray()){
	        int NUMPAD_KEY = getNumpad(charater);
	        if (NUMPAD_KEY != -1){
	            r.keyPress(NUMPAD_KEY);
	            r.keyRelease(NUMPAD_KEY);
	        }
	    }
	    r.keyRelease(KeyEvent.VK_ALT);        
	}

	// Get numpad keyevents
	private int getNumpad(char numberChar){
	    switch (numberChar){
	        case '0' : return KeyEvent.VK_NUMPAD0;
	        case '1' : return KeyEvent.VK_NUMPAD1;
	        case '2' : return KeyEvent.VK_NUMPAD2;
	        case '3' : return KeyEvent.VK_NUMPAD3;
	        case '4' : return KeyEvent.VK_NUMPAD4;
	        case '5' : return KeyEvent.VK_NUMPAD5;
	        case '6' : return KeyEvent.VK_NUMPAD6;
	        case '7' : return KeyEvent.VK_NUMPAD7;
	        case '8' : return KeyEvent.VK_NUMPAD8;
	        case '9' : return KeyEvent.VK_NUMPAD9;
	        default: return -1;
	    }
	}

	void mousemove(int x, int y) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.mouseMove(x, y);    
	}
	
	void mousescroll(int dir) {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.mouseWheel(dir);
	}

	// Click on the screen with Robot 
	void click() {
		if (!WowTabFinder.GetCurrentWindow().contains(wowName))
			return;
		r.delay(500);
		r.mousePress(InputEvent.BUTTON1_DOWN_MASK);
		r.mouseRelease(InputEvent.BUTTON1_DOWN_MASK);
	}

	// Select and copy with Robot
	void selectAndCopy(int x, int y) throws AWTException{
		r.mouseMove(x, y);
		// Double click
		click();
		click();
		r.delay(100);
		sendKeyWithCtrl(KeyEvent.VK_C);
	}
}
