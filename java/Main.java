package wowbot;

public class Main {
	
	public static void main(String[] args)
	{
		System.out.println("Starting wowbot!");
		// Show wow window
		WowTabFinder wowFinder = new WowTabFinder();
		wowFinder.showWowWindow();
		
		// Arguments for bot (bg, faction)
		String bgInput = args.length > 0 ? args[0] : "";
		String factionInput = args.length > 1 ? args[1] : "horde";
		// Start bot
		WowBot wowBot = new WowBot();
		wowBot.startBot(bgInput, factionInput);
	}
}
