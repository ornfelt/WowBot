package wowbot;

import java.awt.AWTException;
import java.awt.Robot;
import java.awt.Toolkit;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.StringSelection;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.net.Socket;
import java.net.InetSocketAddress;
import java.text.SimpleDateFormat;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.Arrays;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

import java.sql.*;

class MousePos {
	  final int x;
	  final int y;
	  MousePos(int x, int y) {this.x=x;this.y=y;}
}

public class wowbot {
	
	/* Variables needed for Robot */
	private Robot r;
	Random rand;
	DateTimeFormatter dtf = DateTimeFormatter.ofPattern("yy/MM/dd");
	LocalDateTime now;

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
	private static final int WSGTIMER = 1900;
	private static final int ABTIMER = 1600;
	private static final int AVTIMER = 2700;
	private static final int WSGTURNTIMERALLY = 500;
	private static final int WSGTURNTIMERHORDE = 450;
	private static final int AVTURNTIMERALLY = 130;
	private static final int AVTURNTIMERHORDE = 70;
	
	// Queue settings
	private static boolean isAcore = true; // AzerothCore / TrinityCore
	private static boolean isArena = false; // Start with BG when random
	private static boolean isGroup = false; // If group queue (BG only)
	private static boolean isLowLevel = false; // If low level (special ordering of BGs)
	private static boolean otherCTA = false; // If other BG than WSG, AB, AV is call to arms 
	private static boolean avCTA = false; // If AV is Call To Arms
	private static boolean abCTA = false; // If AB is Call To Arms
	private static boolean isAlly = false; // Faction
	private static int bgCount = 0; // Keep track of how many BGs / arenas that have been played
	private static int bgCountMax = 6; // Max amount of bgCount before switching to BG / arena
	private static String bgInput = "ra"; // Both random BGs and arena
	//private static String bgInput = "r"; // Random BGs
	//private static String bgInput = "a"; // Random arenas
	private static final String bgTeleSpotHorde = "silvermooncity";
	private static final String bgTeleSpotAlly = "exodar";

	// Horde races
	private static List<Integer> hordeRaces = Arrays.asList(2, 5, 6, 8, 10 );
	// The order of the BGs might change depending on current Call to Arms
	private Map<Object, Object> bgOrderMap;

	wowtabfinder windowFinder;
	
	public wowbot() {
		rand = new Random();
		try {
			r = new Robot();
		} catch (AWTException e) {
			e.printStackTrace();
		}
		
		windowFinder = new wowtabfinder();
		System.out.println("Current window: " + windowFinder.GetCurrentWindow());

		//String myString = "DONE";
		//StringSelection stringSelection = new StringSelection(myString);
		//Clipboard clipboard = Toolkit.getDefaultToolkit().getSystemClipboard();
		//clipboard.setContents(stringSelection, null);
	}
	
	void setServer() {
		// Maybe set isAcore here?
        String server = "127.0.0.1";
        int port = 8085;
        System.out.println("Server online: " + testRealm(server, port));
	}
	
    private static boolean testRealm(String server, int port) {
        Socket socket = new Socket();
        try {
            socket.connect(new InetSocketAddress(server, port), 500); // 500ms timeout
            socket.close();
            return true;
        } catch (Exception e) {
            return false;
        }
    }
	
	void setCTA() {
		// Calculate current call to arms
		// select * from game_event where holiday in (283, 284, 285, 353, 400, 420);
		// The start dates could be fetched through SQL if needed...
		long occurence = 60480;
		long length = 6240;

		// AV: 283
        avCTA = checkCTA("2010-05-07 18:00:00", occurence, length);
		System.out.println("AV CTA: " + avCTA);

		// WSG: 284
        boolean wsgCTA = checkCTA("2010-04-02 18:00:00", occurence, length);
		System.out.println("WSG CTA: " + wsgCTA);

		// AB: 285
		abCTA = checkCTA("2010-04-23 18:00:00", occurence, length);
		System.out.println("AB CTA: " + abCTA);

		// EYE: 353
        boolean eyeCTA = checkCTA("2010-04-30 18:00:00", occurence, length);
		System.out.println("EYE CTA: " + eyeCTA);

		// Strand: 400
		boolean strandCTA = checkCTA("2010-04-09 18:00:00", occurence, length);
		System.out.println("Strand CTA: " + strandCTA);

		// Isle: 420
		boolean isleCTA = checkCTA("2010-04-16 18:00:00", occurence, length);
		System.out.println("Isle CTA: " + isleCTA);
		
		otherCTA = (eyeCTA || strandCTA || isleCTA);
		System.out.println("abCTA: " + abCTA + ", avCTA: " + avCTA + ", otherCTA: " + otherCTA);

		bgOrderMap = new HashMap<Object, Object>() {{
			if (otherCTA) {
				put(0, 2); // WSG 2
				put(1, 3); // AB 3
				put(2, 4); // AV 4
			} else if (avCTA) {
				put(2, 1); // AV 1
				put(0, 2); // WSG 2
				put(1, 3); // AB 3
			} else if (abCTA) {
				put(1, 1); // AB 1
				put(0, 2); // WSG 2
				put(2, 3); // AV 3
			} else {
				put(0, 1); // WSG 1
				put(1, 2); // AB 2
				put(2, 3); // AV 3
			}
		}};
	}
	
	boolean checkCTA(String startTime, long occurence, long length) {
		long currenttime = System.currentTimeMillis() / 1000;
		// Creating a new object of the class Date  
	    //Date currentDate = new Date(currenttime * 1000);
	    //System.out.println("currenttime: " + currenttime);
	    //System.out.println("current date: " + currentDate);
		int MINUTE = 60;

		// Define a date format to parse the start time
        SimpleDateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
        try {
            // Parse the start time string into a Date object
            Date startTimeDate = dateFormat.parse(startTime);
            // Convert the Date object to seconds since the epoch
            long start = startTimeDate.getTime() / 1000;
			//System.out.println("start: " + start);
            return (((currenttime - start) % (occurence * MINUTE)) < (length * MINUTE));

        } catch (Exception e) {
            e.printStackTrace();
        }
        return false;
	}
	
	// To compile: javac -classpath ..\lib\mysql-connector-java-8.0.20.jar;. test.java
	// To run: java -classpath ..\lib\mysql-connector-java-8.0.20.jar;. test
	// Or with eclipse: project properties -> Java build path -> libraries -> classpath -> add external jar
	// For mysql: https://dev.mysql.com/downloads/connector/j/?os=26
	// For mariadb: https://jar-download.com/artifacts/org.mariadb.jdbc
	void setPlayerSettings() {
		Connection connection = null;
		System.out.println("Retrieving player settings...");
        try {
            //Class.forName("com.mysql.cj.jdbc.Driver");
            Class.forName("org.mariadb.jdbc.Driver");
            if (isAcore)
				connection = DriverManager.getConnection(
					//"jdbc:mysql://localhost:3306/acore_characters",
					"jdbc:mariadb://localhost:3306/acore_characters",
					"acore", "acore");
            else
				connection = DriverManager.getConnection(
					//"jdbc:mysql://localhost:3306/characters",
					"jdbc:mariadb://localhost:3306/characters",
					"trinity", "trinity");
 
            int accountId = 1;
            Statement statement = connection.createStatement();
            //ResultSet resultSet = statement.executeQuery("select name, race, level from characters where online = 1 and account = " + accountId);
            ResultSet resultSet = statement.executeQuery("select name, race, level from characters where online = 1");
            String race = "";
            int level = 0;

            // Check if player isn't logged in
            if (!resultSet.next()) {
				System.out.println("Player not logged in. Trying to log in...");
				tryLogin();
				// Execute SQL again
				resultSet = statement.executeQuery("select name, race, level from characters where online = 1 and account = " + accountId);
				// Try one more time
				if (!resultSet.next()) {
					System.out.println("Player still not logged in. Trying to log in once more...");
                    r.delay(1000);
                    sendKey(KeyEvent.VK_ENTER);
					tryLogin();
					// Execute SQL again
					resultSet = statement.executeQuery("select name, race, level from characters where online = 1 and account = " + accountId);
					if (!resultSet.next())
						System.exit(0);
				}
            }

            //while (resultSet.next()) {
            //}

			race = resultSet.getString("race").trim();
			level = resultSet.getInt("level");
			isAlly = !hordeRaces.contains(Integer.parseInt(race));
			isLowLevel = level < 70;
			System.out.println("\nrace: " + race + ", level: " + level);
			System.out.println("isAlly: " + isAlly + ", isLowLevel: " + isLowLevel);

            resultSet.close();
            statement.close();
            connection.close();
        }
        catch (Exception exception) {
            System.out.println(exception);
        }
	}
	
	// Try to login
	void tryLogin() {
		threadSleep(2000);
		// Press enter to get rid of DC message
		sendKey(KeyEvent.VK_ENTER);
		r.delay(1000);
		// Ctrl-a to mark all text 
		sendKeyWithCtrl(KeyEvent.VK_A);
		r.delay(500);
		sendKeys(isAcore ? "acore" : "tcore");
		r.delay(200);
		sendKey(KeyEvent.VK_TAB);
		r.delay(200);
		sendKeys("123");
		r.delay(200);
		sendKey(KeyEvent.VK_ENTER);
		r.delay(5000);
		sendKey(KeyEvent.VK_ENTER);
		r.delay(5000);
	}
	
	// Start BOT
	void startBot(String bgInputArg, String factionInputArg) {
		if (!bgInputArg.equals(""))
			bgInput = bgInputArg;
		//if (!factionInputArg.equals(""))
			//factionInput = factionInputArg;
		//System.out.println("Args: " + bgInput + ", " + factionInputArg);
		//boolean isAlly = factionInput.toLowerCase().equals("ally");
		
		while (true) {
			// Check game and player status
			threadSleep(3000);
			setServer();
			setCTA();
			setPlayerSettings();
			// 5s thread sleep delay
			threadSleep(5000);
			
			switch(bgInput) {
			case "0":
				System.out.println("Starting WSG bot! isAlly: " + isAlly);
				startBgBot(0); // WSG
				break;
			case "1":
				System.out.println("Starting AB bot! isAlly: " + isAlly);
				startBgBot(1); // AB
				break;
			case "2":
				System.out.println("Starting AV bot! isAlly: " + isAlly);
				startBgBot(2); // AV
				break;
			case "ra":
				if (bgCount < bgCountMax && isArena) {
					System.out.println("Starting arena bot! isAlly: " + isAlly);
					startArenaBot(100); // Random arena
				} else if (bgCount < (bgCountMax/2) && !isArena) {
					System.out.println("Starting random BG bot! isAlly: " + isAlly);
					startBgBot(100); // Random BGs
				} else {
					// This means bgCountMax has been reached
					if (isArena) {
						System.out.println("Switching to playing BGs");
						System.out.println("Starting random BG bot! isAlly: " + isAlly);
						startBgBot(100); // Random BGs
					} else {
						System.out.println("Switching to playing arenas");
						System.out.println("Starting arena bot! isAlly: " + isAlly);
						startArenaBot(100); // Random arena
					}
					bgCount = 0;
					isArena = !isArena;
				}
				bgCount++;
				break;
			case "r":
				System.out.println("Starting random BG bot! isAlly: " + isAlly);
				startBgBot(100); // Random BGs
				break;
			case "a": default: 
				System.out.println("Starting arena bot! isAlly: " + isAlly);
				//startArenaBot(0); // 2v2
				//startArenaBot(1); // 3v3
				//startArenaBot(2); // 5v5
				startArenaBot(100); // Random arena
				break;
			}
		}
	}

	// Start Arena BOT
	void startArenaBot(int arenaId) {
		// Settings
		int timeInBg = 0;
		int maxActionTime = 45;
		int bgTimer = 300;

		r.delay(1000);
		// Teleport to arena NPC
		sendKey(KeyEvent.VK_ENTER);
		r.delay(200);
		if (isAlly)
			sendKeys(".go creature 68938"); // select guid from creature where id1=19911; (id from arena npc from wowhead)
		else
			sendKeys(".go creature 4762"); // select guid from creature where id1=19912; (id from arena npc from wowhead)
		sendKey(KeyEvent.VK_ENTER);

		r.delay(5000);
		// /target arena char and interact with him
		sendKey(KeyEvent.VK_ENTER);
		// Enter '/' manually for Linux
		r.delay(200);
		sendKeyWithShift(KeyEvent.VK_7);
		r.delay(60);
		if (isAlly)
			sendKeys("target beka");
		else
			sendKeys("target zeggon");
		sendKey(KeyEvent.VK_ENTER);
		r.delay(700);
		sendKey(KeyEvent.VK_9);
		r.delay(1300);

		if (arenaId == 100) // Hard coded, 100 means random arena
			arenaId = rand.nextInt(3);
		
		System.out.println("Playing arena: " + arenaId);

		if (arenaId == 0)
			mousemove(arena2v2.x, arena2v2.y); // 2v2
		else if (arenaId == 1)
			mousemove(arena3v3.x, arena3v3.y); // 3v3
		else
			mousemove(arena5v5.x, arena5v5.y); // 5v5

		click();
		r.delay(1000);
		mousemove(queueJoin.x, queueJoin.y); // Join queue
		click();
		
		r.delay(3000);
		mousemove(queueAccept.x, queueAccept.y); // Accept queue inv
		click();

		r.delay(5000);
		click();

		// Wait for arena to start...
		for (int i = 0; i < 5; i++) {
			r.delay(9000);
			if (i == 0)
				sendKey(KeyEvent.VK_W, 1000);
			else if (i == 1)
				sendKey(KeyEvent.VK_D, 350);
			else {
				// 20 % chance of jumping, else use spell (scroll down)
				if (rand.nextInt(4) == 0) {
					sendKey(KeyEvent.VK_SPACE);
				} else
					r.mouseWheel(1);
				}
		}
		
		// Random spell casting
		for (int i = 0; i < 80 && timeInBg < bgTimer; i++) {
			r.delay(5000); // 5s delay

			// 20 % chance of jumping, else use spell (scroll down)
			if (rand.nextInt(4) == 0)
				sendKey(KeyEvent.VK_SPACE);
			else
				r.mouseWheel(1);

			r.delay(1500); // 1.5s delay

			if (timeInBg < maxActionTime)
				sendKey(KeyEvent.VK_W);

			r.delay(1500);
			// Use E or 4 spell
			if (timeInBg < maxActionTime) {
				if (rand.nextInt(2) == 0) {
					sendKey(KeyEvent.VK_T);
					r.delay(500);
					sendKey(KeyEvent.VK_E);
				}
				else
					sendKey(KeyEvent.VK_4);
				r.delay(200);
			}

			r.delay(1000);
			if (timeInBg < maxActionTime) {
				// Use R spell
				sendKey(KeyEvent.VK_R);
				r.delay(500);
				// Use 2
				sendKey(KeyEvent.VK_2);
				r.delay(580);
				// Use shift-w
				sendKeyWithShift(KeyEvent.VK_W);
			}

			timeInBg += 11;
			//System.out.println("End of loop... timeInBg: " + timeInBg + ", bgTimer: " + bgTimer);
		}
	}
	
	// Start Battleground BOT
	void startBgBot(int bg) {
		// Settings
		int timeInBg = 0;
		int bgTimer = 0;
		if (bg == 0)
			bgTimer = WSGTIMER;
		else if (bg == 1)
			bgTimer = ABTIMER;
		else
			bgTimer = AVTIMER;

		// Teleport to some place fun
		sendKey(KeyEvent.VK_ENTER);
		r.delay(100);
		if (isAlly)
			sendKeys(".tele " + bgTeleSpotAlly);
		else
			sendKeys(".tele " + bgTeleSpotHorde);
		r.delay(100);
		sendKey(KeyEvent.VK_ENTER);

		r.delay(5000);
		// Open PVP window
		sendKey(KeyEvent.VK_H);
		r.delay(1000);
		mousemove(bgPress.x, bgPress.y); // Press Battlegrounds
		click();
		
		// Handle random BG
		if (bg == 100) // Hard coded, 100 means random arena
			bg = rand.nextInt(3);
		System.out.println("Playing BG: " + bg);

		r.delay(1000);
		if (bg == 0)
			switch ((int)bgOrderMap.get(bg)) {
				case 1:
					mousemove(bg1.x, bg1.y); // WSG 1
					break;
				case 2: default:
					mousemove(bg2.x, bg2.y); // WSG 2
					break;
			}
		else if (bg == 1)
			switch ((int)bgOrderMap.get(bg)) {
				case 1:
					mousemove(bg1.x, bg1.y); // AB 1
					break;
				case 2:
					mousemove(bg2.x, bg2.y); // AB 2
					break;
				case 3: default:
					mousemove(bg3.x, bg3.y); // AB 3
					break;
			}
		else
			switch ((int)bgOrderMap.get(bg)) {
				case 1:
					mousemove(bg1.x, bg1.y); // AV 1
					break;
				case 3:
					mousemove(bg3.x, bg3.y); // AV 3
					break;
				case 4: default:
					mousemove(bg4.x, bg4.y); // AV 4
					break;
			}
		
		// USE THIS IF LOW LEVEL
		if (isLowLevel) {
			if (otherCTA) {
                if (bg == 0)
                    mousemove(bg1.x, bg1.y); // WSG
                else if (bg == 1)
                    mousemove(bg2.x, bg2.y); // AB
                else
                    mousemove(bg3.x, bg3.y); // AV
            } else {
                if (bg == 0)
                    mousemove(lowLevelWsg.x, lowLevelWsg.y); // WSG
                else if (bg == 1)
                    mousemove(bg1.x, bg1.y); // AB
                else
                    mousemove(bg2.x, bg2.y); // AV
            }
		}

		click();

		r.delay(1000);
		if (isGroup)
			mousemove(queueJoin.x-120, queueJoin.y); // Join group queue
		else
			mousemove(queueJoin.x, queueJoin.y); // Join queue

		click();
		
		r.delay(3000);
		mousemove(queueAccept.x, queueAccept.y); // Accept queue inv
		click();

		r.delay(5000);
		click();

		// Wait for BG to start...
		if (bg == 0) {
			r.delay(1000);
			sendKey(KeyEvent.VK_D, 500);
			r.delay(500);
			sendKey(KeyEvent.VK_W, 1700);
			r.delay(1000);

			// Turn slightly in WSG beginning
			sendKey(KeyEvent.VK_A, isAlly ? WSGTURNTIMERALLY : WSGTURNTIMERHORDE);

			r.delay(500);
			sendKey(KeyEvent.VK_W, 1500);
			r.delay(46000);
		} else {
			for (int i = 0; i < 5; i++) {
				r.delay(9000);
				sendKey(KeyEvent.VK_W, 1000);

				// Turn slightly in AV beginning
				if (bg == 2 && i == 0) {
					r.delay(100);
					sendKey(KeyEvent.VK_A, 100);
				} else if (bg == 2 && i == 4) {
					r.delay(100);
					sendKey(KeyEvent.VK_D, isAlly ? AVTURNTIMERALLY : AVTURNTIMERHORDE);
				}
			}
		}
		
		// Random walking and some spell casts
		for (int i = 0; i < 100 && timeInBg < bgTimer; i++) {
			r.delay(2000);
			sendKey(KeyEvent.VK_W);
			
			// Auto run
			r.delay(500);
			sendKeyWithAlt(KeyEvent.VK_X);
			
			r.delay(9000);
			sendKey(KeyEvent.VK_T);
			r.delay(400);
			// 20 % chance of jumping, else use spell (scroll down)
			//if (rand.nextInt(10) == 0) // 10 % chance
			if (rand.nextInt(4) == 0) {
				sendKey(KeyEvent.VK_SPACE);
				r.delay(500);
			} else
				r.mouseWheel(1);

			r.delay(1500);
			sendKey(KeyEvent.VK_W);

			// 50 % chance of turning left / right
			// Don't turn in AB / AV until a few minutes have passed 
			if (bg == 0 || timeInBg > 150) {
				if (rand.nextInt(2) == 0) {
					//System.out.println("Turning left");
					sendKey(KeyEvent.VK_A, 500);
					// 50 % chance of turning some more
					r.delay(100);
					if (rand.nextInt(2) == 0)
						sendKey(KeyEvent.VK_A, 200);
					else
						// Else use 2
						sendKey(KeyEvent.VK_2);
				}
				else {
					//System.out.println("Turning right");
					sendKey(KeyEvent.VK_D, 500);
					r.delay(100);
					// 50 % chance of turning some more
					if (rand.nextInt(2) == 0)
						sendKey(KeyEvent.VK_D, 200);
					else
						// Else use 4
						sendKey(KeyEvent.VK_4);
				}
			}
			
			// 30 % chance of clicking release and wait for 30 sec
			if (rand.nextInt(3) == 0) {
				//System.out.println("Trying to release... Loop count: " + i);
				// First try to accept ress from someone, then try to release
				r.delay(500);
				mousemove(queueAccept.x, queueAccept.y);
				click();
				r.delay(500);
				mousemove(acceptRess.x, acceptRess.y);
				click();
				// Try clicking a bit further down as well since
				// release button can be moved down if bot 
				// ressed player but it expired before getting accepted
				r.delay(500);
				mousemove(acceptRess.x, acceptRess.y+70);
				click();

				// Wait 30 sec
				r.delay(12000);
				sendKey(KeyEvent.VK_W);
				r.delay(15000);
				timeInBg += 30;
				// Also use shift-w
				sendKeyWithShift(KeyEvent.VK_SHIFT);
			}
			timeInBg += 14;

			// Use R spell
			sendKey(KeyEvent.VK_R);
			//System.out.println("End of loop... timeInBg: " + timeInBg + ", bgTimer: " + bgTimer);
		}
		if (bg == 2)
			System.out.println("End of AV loop... timeInBg: " + timeInBg);
	}
	
	// Thread.Sleep for x amount of time
	void threadSleep(int sleepTime) {
		try {
			Thread.sleep(sleepTime);
		} catch (InterruptedException e) {
			e.printStackTrace();
		}
	}
	
	// Execute specific key
	void sendKey(int key) {
		if (!windowFinder.GetCurrentWindow().contains("World of Warcraft"))
			return;
		r.keyPress(key);
		r.delay(60);
		r.keyRelease(key);
		r.delay(60);
	}

	void sendKey(int key, int delay) {
		if (!windowFinder.GetCurrentWindow().contains("World of Warcraft"))
			return;
		r.keyPress(key);
		r.delay(delay);
		r.keyRelease(key);
		r.delay(60);
	}

	void sendKeyWithShift(int key) {
		if (!windowFinder.GetCurrentWindow().contains("World of Warcraft"))
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
		if (!windowFinder.GetCurrentWindow().contains("World of Warcraft"))
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
		if (!windowFinder.GetCurrentWindow().contains("World of Warcraft"))
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
	    		keyPress('/');
	    	}else if (c == ':') {
	    		keyPress(':');
	    	}else if (c == '@') {
	    		keyPress('@');
	    	}else {
				int keyCode = KeyEvent.getExtendedKeyCodeForChar(c);
				if (KeyEvent.CHAR_UNDEFINED == keyCode) {
					throw new RuntimeException(
						"Key code not found for character '" + c + "'");
				}
				r.delay(100);
				r.keyPress(keyCode);
				r.delay(100);
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
		if (!windowFinder.GetCurrentWindow().contains("World of Warcraft"))
			return;
		r.mouseMove(x, y);    
	}

	// Click on the screen with Robot 
	void click() {
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
