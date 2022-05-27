using Discord;

public class RDR2ShowdownDetector : RDR2ActivityDetector
{
    private static string[] SCREEN_LOBBY_INDICATORS = new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam" };
    private static string[] SCREEN_END_INDICATORS = new string[] { "MATCH OVER", "MATCH WON", "MATCH LOST" };
    private static string[] SCREEN_SCORE_INDICATORS = new string[] { "VOTE LIKE", "VOTE DISLIKE", "CONTINUE" };
    private static long DURATION = 1000 * 60 * 10;

    public delegate string? Produce(string text);

    private string name;
    private Produce stateProducer;
    
    private bool active, inGame;
    private string? state;
    private long start;

    public RDR2ShowdownDetector(string name, Produce state)
    {
        this.name = name;
        this.stateProducer = state;
        this.active = false;
        this.inGame = false;
        this.state = null;
        this.start = 0;
    }

    public override bool IsActive()
    {
        return active;
    }

    public override void Parse(string text)
    {
        if (active && Environment.TickCount64 - start > DURATION + (1000 * 60 * 3 /* to compensate for loading */)) active = false;
        else if (active && !inGame) inGame = !SCREEN_LOBBY_INDICATORS.Any(indicator => text.FuzzyContains(indicator, 0.9));
        else if (active && inGame) active = !(SCREEN_END_INDICATORS.Any(indicator => text.FuzzyContains(indicator, 0.9)) || SCREEN_SCORE_INDICATORS.Any(indicator => text.FuzzyContains(indicator, 0.9)));
        else
        {
            active = text.FuzzyContains(name.ToUpper(), 0.9) && SCREEN_LOBBY_INDICATORS.All(indicator => text.FuzzyContains(indicator, 0.9));
            if (active) start = Environment.TickCount64;
            if (active) inGame = false;
            if (active) state = null;
        }
        if (active && inGame) state = stateProducer.Invoke(text) ?? state;
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create("Showdown: " + name, inGame ? state ?? "Competing" : "Preparing");
    }
}
