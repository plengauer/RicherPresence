using Discord;

public class RDR2GenericMissionDetector : RDR2ActivityDetector
{

    public delegate string? Produce(string text);

    private string[] startIndicators;
    private string[] endIndicators;
    private long duration;
    private Produce detailsProducer, stateProducer;
    
    private bool active;
    private string? details, state;
    private long start;

    public RDR2GenericMissionDetector(string startIndicator, string endIndicator, long duration, Produce details, Produce state) : this(new string[] { startIndicator }, new string[] { endIndicator }, duration, details, state)
    {}

    public RDR2GenericMissionDetector(string[] startIndicators, string[] endIndicators, long duration, Produce details, Produce state)
    {
        this.startIndicators = startIndicators;
        this.endIndicators = endIndicators.Concat(new string[] { "MISSION OVER", "MISSION PASSED", "MISSION FAILED", "MATCH OVER", "MATCH WON", "MATCH LOST", "VOTE LIKE", "VOTE DISLIKE", "CONTINUE", "YOUR POSSE ABANDONED THE JOB" }).ToArray();
        this.duration = duration;
        this.detailsProducer = details;
        this.stateProducer = state;
        this.active = false;
        this.details = null;
        this.state = null;
        this.start = 0;
    }

    public override bool IsActive()
    {
        return active;
    }

    public override void Parse(string text)
    {
        if (active && Environment.TickCount64 - start > duration + (1000 * 60 * 3 /* to compensate for loading */)) active = false;
        else if (active) active = !endIndicators.Any(indicator => text.FuzzyContains(indicator, 0.9));
        else
        {
            active = startIndicators.All(indicator => text.FuzzyContains(indicator, 0.9)) && endIndicators.All(indicator => !text.FuzzyContains(indicator, 0.9));
            if (active) start = Environment.TickCount64;
            if (active) details = state = null;
        }
        if (active) details = detailsProducer.Invoke(text) ?? details;
        if (active) state = stateProducer.Invoke(text) ?? state;
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create(details, state);
    }
}
