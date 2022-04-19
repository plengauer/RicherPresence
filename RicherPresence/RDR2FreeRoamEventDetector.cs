using Discord;

public class RDR2FreeRoamEventDetector : RDR2ActivityDetector
{
    private static string[] SCREEN_END_INDICATORS = new string[] { "MATCH OVER", "MATCH WON", "MATCH LOST" };
    private static string[] SCREEN_SCORE_INDICATORS = new string[] { "VOTE LIKE", "VOTE DISLIKE", "CONTINUE" };
    private static long DURATION = 1000 * 60 * 10;

    public delegate string? Produce(string text);

    private string name, instructions;
    private Produce stateProducer;
    
    private bool active;
    private string? state;
    private long start;

    public RDR2FreeRoamEventDetector(string name, string instructions, Produce state)
    {
        this.name = name;
        this.instructions = instructions;
        this.stateProducer = state;
        this.active = false;
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
        else if (active) active = !(text.FuzzyContains(name.ToUpper() + " OVER", 0.99) || SCREEN_END_INDICATORS.Any(indicator => text.FuzzyContains(indicator, 0.9)) || SCREEN_SCORE_INDICATORS.Any(indicator => text.FuzzyContains(indicator, 0.9)));
        else
        {
            active = (text.FuzzyContains(name.ToUpper(), 0.9) && text.FuzzyContains(instructions.ToUpper(), 0.9)) || text.FuzzyContains("Wait for " + name + " to start", 0.9);
            if (active) start = Environment.TickCount64;
            if (active) state = null;
        }
        if (active) state = stateProducer.Invoke(text) ?? state;
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create("Event: " + name, state ?? "Competing");
    }
}
