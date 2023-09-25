package wowbot;

public class main {
	
	public static void main(String[] args)
	{
		System.out.println("Starting wowbot!");
		// Show wow window (done manually on linux for now)
		//WowTabFinder wowFinder = new WowTabFinder();
		//wowFinder.showWowWindow();
		
		// Arguments for bot (bg, faction)
		String bgInput = args.length > 0 ? args[0] : "";
		String factionInput = args.length > 1 ? args[1] : "horde";
		// Start bot
		wowbot wowBot = new wowbot();
		wowBot.startBot(bgInput, factionInput);
	}
}
