package wowbot;

public class Main {
	
	// Usage: java -jar wowbot.jar 1 1
	public static void main(String[] args)
	{
		boolean isLinux = false; // Windows per default
		boolean isAcore = true; // AzerothCore per default
		for (int i = 0; i < args.length; i++)
			System.out.println("arg " + i + ": " + args[i]);
		isLinux = args.length > 0 && args[0].contains("1");
		isAcore = args.length > 1 && args[1].contains("1");

		// Start BOT
		System.out.println("Starting wowbot!");
		WowBot bot = new WowBot(isLinux, isAcore);
		bot.startBot();
	}
}