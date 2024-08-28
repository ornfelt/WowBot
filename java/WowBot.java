package wowbot;

import java.awt.AWTException;
import java.awt.Robot;
import java.awt.event.KeyEvent;
import java.net.Socket;
import java.net.InetSocketAddress;
import java.text.SimpleDateFormat;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.Arrays;
import java.util.Date;
import java.util.List;
import java.util.Random;
import java.util.HashMap;
import java.util.Map;

import java.sql.*;

class MousePos {
	  final int x;
	  final int y;
	  MousePos(int x, int y) {this.x=x;this.y=y;}
}

public class WowBot {
	
	/* Variables needed for Robot */
	private static Robot r;
	private static InputManager inputManager;
	private static Random rand;
	private static DateTimeFormatter dtf = DateTimeFormatter.ofPattern("yy/MM/dd");
	private static LocalDateTime now;

	// Configuration (mouse clicks not used anymore)
	private static MousePos arena2v2;
	private static MousePos arena3v3;
	private static MousePos arena5v5;
	private static MousePos queueJoin;
	private static MousePos queueAccept;
	private static MousePos bgPress;
	private static MousePos bg1;
	private static MousePos bg2;
	private static MousePos bg3;
	private static MousePos bg4;
	private static MousePos lowLevelWsg;
	private static MousePos acceptRess;

	// Timers
	//private static final int WSGTIMER = 1800;
	//private static final int ABTIMER = 1600;
	//private static final int AVTIMER = 2800;
	private static final int WSGTIMER = 4000;
	private static final int ABTIMER = 4000;
	private static final int AVTIMER = 4000;
	private static int wsgTurnTimerAlly;
	private static int wsgTurnTimerHorde;
	private static int avTurnTimerAlly;
	private static int avTurnTimerHorde;
	
	// Settings
	private static boolean isAcore = true; // AzerothCore or TrinityCore
	private static boolean isLinux = false; // Linux or Windows
	private static boolean isLocalServer = true; // Connecting to local server or not
	
	private static boolean isArena = false; // Start with BG when random
	private static boolean isGroup = false; // If group queue (BG only)
	private static boolean otherCTA = false; // If other BG than WSG, AB, AV is call to arms 
	private static boolean avCTA = false; // If AV is Call To Arms
	private static boolean abCTA = false; // If AB is Call To Arms
	private static boolean eyeCTA, strandCTA, isleCTA;
	private static boolean isAlly = false; // Faction

    private static boolean isAlreadyInBg = false;

	private static int bgCount = 0; // Keep track of how many BGs / arenas that have been played
	private static int bgCountMax = 6; // Max amount of bgCount before switching to BG / arena
	private static int playerLevel = 0; // Player level
	private static String bgInput = "ra"; // Both random BGs and arena
	//private static String bgInput = "r"; // Random BGs
	//private static String bgInput = "a"; // Random arenas
    private static final String[] bgTeleSpotHorde = {"silvermooncity", "orgrimmar", /*"thunderbluff",*/ "undercity", "dalaran", "shattrath"};
    private static final String[] bgTeleSpotAlly = {"exodar", "stormwind", "darnassus", /*"ironforge",*/ "dalaran", "shattrath"};

	// Horde races
	private static List<Integer> hordeRaces = Arrays.asList(2, 5, 6, 8, 10 );
	
	public WowBot(boolean isLinuxArg, boolean isAcoreArg, String nonLocalServerSettingsArg) {
        initSettings();
		rand = new Random();
		try {
			r = new Robot();
		} catch (AWTException e) {
			e.printStackTrace();
		}
		isLinux = isLinuxArg;
		isAcore = isAcoreArg;
        if (!nonLocalServerSettingsArg.equals("")) {
        	isLocalServer = false;
            String[] parts = nonLocalServerSettingsArg.split(",");

            if (parts.length >= 2) {
                isAlly = parts[0].equals("1");

                try {
                    playerLevel = Integer.parseInt(parts[1]);
                } catch (NumberFormatException e) {
                    System.err.println("Error: The second part of nonLocalServerSettingsArg is not a valid integer.");
                }
            } else {
                System.err.println("Error: nonLocalServerSettingsArg does not contain two values separated by a comma. Please provide isAlly and playerLevel like this: 0,80");
                System.exit(0);
            }
        }
		System.out.println("isLinux: " + isLinux + ", isAcore: " + isAcore + ", nonLocalServerSettings: " + nonLocalServerSettingsArg);
		inputManager = new InputManager(r, isLinux);
		
		//String myString = "DONE";
		//StringSelection stringSelection = new StringSelection(myString);
		//Clipboard clipboard = Toolkit.getDefaultToolkit().getSystemClipboard();
		//clipboard.setContents(stringSelection, null);
	}
	
	private static void initSettings() {
        if (isLinux) {
            arena2v2 = new MousePos(240, 408);
            arena3v3 = new MousePos(240, 420);
            arena5v5 = new MousePos(240, 440);
            queueJoin = new MousePos(290, 662);
            queueAccept = new MousePos(850, 265);
            bgPress = new MousePos(180, 695);
            bg1 = new MousePos(240, 285);
            bg2 = new MousePos(240, 308);
            bg3 = new MousePos(240, 330);
            bg4 = new MousePos(240, 340);
            lowLevelWsg = new MousePos(240, 270);
            acceptRess = new MousePos(900, 265);
            wsgTurnTimerAlly = 500;
            wsgTurnTimerHorde = 450;
            avTurnTimerAlly = 130;
            avTurnTimerHorde = 70;
        } else {
            arena2v2 = new MousePos(240, 320);
            arena3v3 = new MousePos(240, 335);
            arena5v5 = new MousePos(240, 350);
            queueJoin = new MousePos(290, 505);
            queueAccept = new MousePos(710, 220);
            bgPress = new MousePos(200, 530);
            bg1 = new MousePos(240, 240);
            bg2 = new MousePos(240, 250);
            bg3 = new MousePos(240, 260);
            bg4 = new MousePos(240, 280);
            lowLevelWsg = new MousePos(240, 220);
            acceptRess = new MousePos(730, 220);
            wsgTurnTimerAlly = 495;
            wsgTurnTimerHorde = 455;
            avTurnTimerAlly = 150;
            avTurnTimerHorde = 80;
        }
	}
	
	private static void setServer() {
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
        eyeCTA = checkCTA("2010-04-30 18:00:00", occurence, length);
		System.out.println("EYE CTA: " + eyeCTA);

		// Strand: 400
		strandCTA = checkCTA("2010-04-09 18:00:00", occurence, length);
		System.out.println("Strand CTA: " + strandCTA);

		// Isle: 420
		isleCTA = checkCTA("2010-04-16 18:00:00", occurence, length);
		System.out.println("Isle CTA: " + isleCTA);
		
		otherCTA = (eyeCTA || strandCTA || isleCTA);
		System.out.println("abCTA: " + abCTA + ", avCTA: " + avCTA + ", otherCTA: " + otherCTA);
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
	// For mysql: https://dev.mysql.com/downloads/connector/j/
	// For mariadb: https://jar-download.com/artifacts/org.mariadb.jdbc
	void setPlayerSettings() {
		Connection connection = null;
		System.out.println("Retrieving player settings...");
        try {
			if (isLinux)
				Class.forName("org.mariadb.jdbc.Driver");
			else
				Class.forName("com.mysql.cj.jdbc.Driver");
			if (isAcore)
				connection = DriverManager.getConnection(
					isLinux ? "jdbc:mariadb://localhost:3306/acore_characters" :
					"jdbc:mysql://localhost:3306/acore_characters",
					"acore", "acore");
            else
				connection = DriverManager.getConnection(
					isLinux ? "jdbc:mariadb://localhost:3306/characters" :
					"jdbc:mysql://localhost:3306/characters",
					"trinity", "trinity");
 
            //int accountId = 1;
			//String sqlQuery = "select name, race, level from characters where online = 1 and account = " + accountId;
			String sqlQuery = "select name, race, level from characters where online = 1";
            Statement statement = connection.createStatement();
            ResultSet resultSet = statement.executeQuery(sqlQuery);
            String race = "";

            // Ensure player logged in
            if (!resultSet.next()) {
				System.out.println("Player not logged in. Trying to log in...");
				tryLogin();
				// Execute SQL again
				resultSet = statement.executeQuery(sqlQuery);
				// Try two more times
				if (!resultSet.next()) {
					System.out.println("Player still not logged in. Trying to log in again...");
					r.delay(1000);
					inputManager.sendKey(KeyEvent.VK_ENTER);
					tryLogin();
					// Execute SQL again
					resultSet = statement.executeQuery(sqlQuery);
					if (!resultSet.next()) {
						System.out.println("Player still not logged in. Trying to log in once more...");
						r.delay(1000);
						tryLogin();
						// Execute SQL again
						resultSet = statement.executeQuery(sqlQuery);
						if (!resultSet.next())
							System.exit(0);
					}
				}
            }

            //while (resultSet.next()) {
            //}

			race = resultSet.getString("race").trim();
			playerLevel = resultSet.getInt("level");
			isAlly = !hordeRaces.contains(Integer.parseInt(race));
			System.out.println("\nrace: " + race + ", level: " + playerLevel + ", isAlly: " + isAlly);

            resultSet.close();
            statement.close();
            connection.close();
        }
        catch (Exception exception) {
            System.out.println(exception);
        }
	}

    boolean isPlayerInBgMap() {
        Connection connection = null;
        System.out.println("Checking player map...");
        try {
            if (isLinux)
                Class.forName("org.mariadb.jdbc.Driver");
            else
                Class.forName("com.mysql.cj.jdbc.Driver");
            if (isAcore)
                connection = DriverManager.getConnection(
                        isLinux ? "jdbc:mariadb://localhost:3306/acore_characters" :
                        "jdbc:mysql://localhost:3306/acore_characters",
                        "acore", "acore");
            else
                connection = DriverManager.getConnection(
                        isLinux ? "jdbc:mariadb://localhost:3306/characters" :
                        "jdbc:mysql://localhost:3306/characters",
                        "trinity", "trinity");

            String sqlQuery = "select map from characters where online = 1";
            Statement statement = connection.createStatement();
            ResultSet resultSet = statement.executeQuery(sqlQuery);

            // Did we get a result? Required check since otherwise resultSet will be empty (if you don't do .next()...)
            if (!resultSet.next()) {
                return true; // Return true to make sure loop continues...
            }

            int map = resultSet.getInt("map");
            System.out.println("Player is in map: " + map + " (" + bgMap.getOrDefault(map, "Unknown") + ")");

            if (map != 30 && map != 489 && map != 529 && map != 559) {
                System.out.println("Player is NOT in BG!");
                return false;
            }

            resultSet.close();
            statement.close();
            connection.close();

            return true;
        }
        catch (Exception exception) {
            System.out.println(exception);
            return true; // Return true to make sure loop continues...
        }
    }
	
	// Try to login
	void tryLogin() {
		threadSleep(2000);
		// Press enter to get rid of DC message
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(1000);
		// Ctrl-a to mark all text 
		inputManager.sendKeyWithCtrl(KeyEvent.VK_A);
		r.delay(500);
		inputManager.sendKeys(isAcore ? "acore" : "tcore");
		r.delay(200);
		inputManager.sendKey(KeyEvent.VK_TAB);
		r.delay(200);
		inputManager.sendKeys("123");
		r.delay(200);
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(5000);
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(8000);
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(50);
		inputManager.sendKey(KeyEvent.VK_ENTER);
		threadSleep(10000);
	}
	
	// Start BOT
	void startBot() {
        // Check if player is already in bg
        if (isLocalServer)
            isAlreadyInBg = isPlayerInBgMap();

		while (true) {
			WowTabFinder.showWowWindow();
			// Check game and player status
			threadSleep(3000);
			setServer();
			setCTA();
			// Set player settings through local DB - unless player settings are already set through nonLocalServerSettingsArg
			if (isLocalServer)
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
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(500);
		if (isAlly)
			inputManager.sendKeys(".go creature 68938"); // select guid from creature where id1=19911; (id from arena npc from wowhead)
		else
			inputManager.sendKeys(".go creature 4762"); // select guid from creature where id1=19912; (id from arena npc from wowhead)
		inputManager.sendKey(KeyEvent.VK_ENTER);

		r.delay(15000);
		// /target arena char and interact with him
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(500);
		if (isAlly)
			inputManager.sendKeys("/target beka");
		else
			inputManager.sendKeys("/target zeggon");
		r.delay(500);
		inputManager.sendKey(KeyEvent.VK_ENTER);
		r.delay(700);
		inputManager.sendKey(KeyEvent.VK_9);
		r.delay(1300);

		if (arenaId == 100) // Hard coded, 100 means random arena
			arenaId = rand.nextInt(3)+1;
		
		if (arenaId == 3) // Extend bgTimer slightly for 5v5
			bgTimer += 50;
		
		System.out.println("Playing arena: " + arenaId);
        System.out.println("arena type: " + arenaMap.getOrDefault(arenaId, "Unknown"));

		inputManager.joinBattlefield(arenaId, isGroup);
		r.delay(1000);
		inputManager.clickPopup(); // Accept queue
		r.delay(5000);

		// Wait for arena to start...
		for (int i = 0; i < 5; i++) {
			r.delay(9000);
			if (i == 0)
				inputManager.sendKey(KeyEvent.VK_W, 1000);
			else if (i == 1)
				inputManager.sendKey(KeyEvent.VK_D, 350);
			else {
				// 20 % chance of jumping, else use spell (scroll down)
				if (rand.nextInt(4) == 0)
					inputManager.sendKey(KeyEvent.VK_SPACE);
				else
					inputManager.mousescroll(i);
			}
		}
		
		// Random spell casting
		for (int i = 0; i < 80 && timeInBg < bgTimer; i++) {
			r.delay(5000); // 5s delay

			// 20 % chance of jumping, else use spell (scroll down)
			if (rand.nextInt(4) == 0)
				inputManager.sendKey(KeyEvent.VK_SPACE);
			else
				inputManager.mousescroll(i);

			r.delay(1500); // 1.5s delay
			if (timeInBg < maxActionTime)
				inputManager.sendKey(KeyEvent.VK_W);
			r.delay(1500);
			
			// Use E or 4 spell
			if (timeInBg < maxActionTime) {
				if (rand.nextInt(2) == 0) {
					inputManager.sendKey(KeyEvent.VK_T);
					inputManager.sendKey(KeyEvent.VK_E);
				}
				else
					inputManager.sendKey(KeyEvent.VK_4);
			}

			r.delay(1500);
			if (timeInBg < maxActionTime) {
				// Use R spell
				inputManager.sendKey(KeyEvent.VK_R);
				r.delay(500);
				// Use 2
				inputManager.sendKey(KeyEvent.VK_2);
				r.delay(500);
				// Use shift-w
				inputManager.sendKeyWithShift(KeyEvent.VK_W);
			}

			timeInBg += 11;
			//System.out.println("End of loop... timeInBg: " + timeInBg + ", bgTimer: " + bgTimer);

            if (isLocalServer && i > 3 && !isPlayerInBgMap())
                break;
		}

        System.out.println("Arena loop finished... timeInBg: " + timeInBg);
	}

    // Get random teleport destination based on some capital cities
    private static String getRandomTeleportSpot() {
        return isAlly ? bgTeleSpotAlly[rand.nextInt(bgTeleSpotAlly.length)] : bgTeleSpotHorde[rand.nextInt(bgTeleSpotHorde.length)];
    }
	
	// Start Battleground BOT
	void startBgBot(int bg) {
		// Settings
		int timeInBg = 0;
		int bgTimer = 0;

        if (!isAlreadyInBg) {
            // Teleport to some place fun
            inputManager.sendKey(KeyEvent.VK_ENTER);
            r.delay(500);
            inputManager.sendKeys(".tele " + getRandomTeleportSpot());
            r.delay(500);
            inputManager.sendKey(KeyEvent.VK_ENTER);
            r.delay(12000);

            // Handle random BG
            if (bg == 100) // Hard coded, 100 means random arena
                bg = (playerLevel < 20) ? 0 : (playerLevel < 51) ? rand.nextInt(2) : rand.nextInt(3);

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

            System.out.println("Queueing for bg: " + bg + ", bgQueueIndex: " + bgQueueIndex);
            System.out.println("bg name: " + simpleBgMap.getOrDefault(bg, "Unknown"));

            // Join BG
            //inputManager.selectBg(bgQueueIndex);
            inputManager.selectBg(bg); // Use lua instead
            inputManager.joinBattlefield(0, isGroup);
            inputManager.clickPopup(); // Accept queue
            r.delay(7000);

            // Wait for BG to start...
            if (bg == 0) {
                r.delay(1000);
                inputManager.sendKey(KeyEvent.VK_D, 500);
                r.delay(500);
                inputManager.sendKey(KeyEvent.VK_W, 1700);
                r.delay(1000);

                // Turn slightly in WSG beginning
                inputManager.sendKey(KeyEvent.VK_A, isAlly ? wsgTurnTimerAlly : wsgTurnTimerHorde);

                r.delay(500);
                inputManager.sendKey(KeyEvent.VK_W, 1500);
                r.delay(46000);
            } else {
                for (int i = 0; i < 5; i++) {
                    r.delay(9000);
                    inputManager.sendKey(KeyEvent.VK_W, 1000);
                    r.delay(100);

                    // Turn slightly in AV beginning
                    if (bg == 2 && i == 0)
                        inputManager.sendKey(KeyEvent.VK_A, 100);
                    else if (bg == 2 && i == 4)
                        inputManager.sendKey(KeyEvent.VK_D, isAlly ? avTurnTimerAlly : avTurnTimerHorde);
                }
            }
        } else {
            System.out.println("Player is already in BG. Jumping to random bot bg behaviour...");
            bgTimer = AVTIMER;
            isAlreadyInBg = false;
        }
		
		// Random walking and some spell casts
		for (int i = 0; i < 100 && timeInBg < bgTimer; i++) {
			r.delay(2000);
			inputManager.sendKey(KeyEvent.VK_W);
			
			// Auto run
			r.delay(500);
			inputManager.sendKeyWithAlt(KeyEvent.VK_X);
			r.delay(9000);
			inputManager.sendKey(KeyEvent.VK_T);
			r.delay(500);
			
			// 20 % chance of jumping, else use spell (scroll down)
			if (rand.nextInt(4) == 0)
				inputManager.sendKey(KeyEvent.VK_SPACE);
			else
				inputManager.mousescroll(i);

			r.delay(2000);
			inputManager.sendKey(KeyEvent.VK_W);

			// 50 % chance of turning left / right
			// Don't turn in AB / AV until a few minutes have passed 
			if (bg == 0 || timeInBg > 150) {
				if (rand.nextInt(2) == 0) {
					//System.out.println("Turning left");
					inputManager.sendKey(KeyEvent.VK_A, 500);
					// 50 % chance of turning some more
					r.delay(100);
					if (rand.nextInt(2) == 0)
						inputManager.sendKey(KeyEvent.VK_A, 200);
					else
						// Else use 2
						inputManager.sendKey(KeyEvent.VK_2);
				}
				else {
					//System.out.println("Turning right");
					inputManager.sendKey(KeyEvent.VK_D, 500);
					r.delay(100);
					// 50 % chance of turning some more
					if (rand.nextInt(2) == 0)
						inputManager.sendKey(KeyEvent.VK_D, 200);
					else
						// Else use 4
						inputManager.sendKey(KeyEvent.VK_4);
				}
			}
			
			// 30 % chance of inputManager.clicking release and wait for 30 sec
			if (rand.nextInt(3) == 0) {
				r.delay(500);
				inputManager.clickPopup();
				// Wait ~30 sec
				r.delay(13000);
				inputManager.sendKey(KeyEvent.VK_W);
				r.delay(15000);
				timeInBg += 30;
				// Use shift-w
				inputManager.sendKeyWithShift(KeyEvent.VK_SHIFT);
			}
			timeInBg += 14;

			// Use R spell
			inputManager.sendKey(KeyEvent.VK_R);
			//System.out.println("End of loop... timeInBg: " + timeInBg + ", bgTimer: " + bgTimer);

            if (isLocalServer && i > 3 && !isPlayerInBgMap())
                break;
		}

        System.out.println("BG loop finished... timeInBg: " + timeInBg);
    }
	
	// Thread.Sleep for x amount of time
	void threadSleep(int sleepTime) {
		try {
			Thread.sleep(sleepTime);
		} catch (InterruptedException e) {
			e.printStackTrace();
		}
	}

    // HashMaps useful for printing...
    // MapId -> MapName
    private static final Map<Integer, String> bgMap = new HashMap<>();
    static {
        bgMap.put(0, "Eastern Kingdoms");
        bgMap.put(1, "Kalimdor");
        bgMap.put(30, "Alterac Valley");
        bgMap.put(489, "Warsong Gulch");
        bgMap.put(529, "Arathi Basin");
        bgMap.put(530, "Outland");
        bgMap.put(559, "Nagrand Arena");
        bgMap.put(562, "Blade's Edge Arena");
        bgMap.put(566, "Eye of the Storm");
        bgMap.put(571, "Northrend");
        bgMap.put(572, "Ruins of Lordaeron");
        bgMap.put(607, "Strand of the Ancients");
        bgMap.put(628, "Isle of Conquest");
    }

    // Get BG name based on random bg index
    private static final Map<Integer, String> simpleBgMap = new HashMap<>();
    static {
        simpleBgMap.put(0, "Warsong Gulch");
        simpleBgMap.put(1, "Arathi Basin");
        simpleBgMap.put(2, "Alterac Valley");
    }

    // Get arena name based on random arena index
    private static final Map<Integer, String> arenaMap = new HashMap<>();
    static {
        arenaMap.put(1, "2v2");
        arenaMap.put(2, "3v3");
        arenaMap.put(3, "5v5");
    }
}
