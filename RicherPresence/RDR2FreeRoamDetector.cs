using Discord;

public class RDR2FreeRoamDetector : RDR2ActivityDetector
{ 

    private RDR2InfoParser location;

    private string bounty;
    private long actionTime;
    private string actionString;

    public RDR2FreeRoamDetector(RDR2InfoParser location)
    {
        this.location = location;
    }

    public override bool IsActive()
    {
        return location.Get() != null || GetAction() != null;
    }

    private string GetAction()
    {
        return actionString != null && (Environment.TickCount64 - actionTime) < 1000 * 60 * 1 ? actionString : null;
    }

    public override void Parse(string text)
    {
        ParseAction(text);
        ParseBounty(text);
    }

    private void ParseAction(string text)
    {
        string newActionString = null;
        if (text.FuzzyContains("STRUGGLE", 0.9) || text.FuzzyContains("BEAT", 0.9) || text.FuzzyContains("SUBDUE", 0.9)) newActionString = "Brawling";
        else if (text.FuzzyContains("SKIN", 0.9)) newActionString = "Hunting";
        else if (text.FuzzyContains("REEL", 0.9)) newActionString = "Fishing";
        else if (text.FuzzyContains("CRAFT", 0.9)) newActionString = "Crafting";
        else if (text.FuzzyContains("TRACK", 0.9) || text.FuzzyContains("FOCUS ON", 0.9)) newActionString = "Tracking";
        else if (text.FuzzyContains("LOOT", 0.9)) newActionString = "Looting";
        // else if (text.FuzzyContains("INVESTIGATING", 0.9)) newActionString = "Being investigated";
        // else if (text.FuzzyContains("WITNESS", 0.9)) newActionString = "Being snitched on for " + ParseCrime(text);
        // else if (text.FuzzyContains("WANTED", 0.9)) newActionString = "Wanted for " + ParseCrime(text);
        else if (text.FuzzyContains("CLEAN", 0.9)) newActionString = "Maintaining Weapons";
        else if (text.FuzzyContains("EAT", 0.9) && !text.FuzzyContains("SEAT", 0.95)) newActionString = "Eating";
        else if (text.FuzzyContains("DRINK", 0.9)) newActionString = "Drinking";
        else if (text.FuzzyContains("BUY", 0.9) || text.FuzzyContains("SELL", 0.9) || text.FuzzyContains("CATALOGUE", 0.9)) newActionString = "Shopping";
        else if (text.FuzzyContains("ADD FLAVOR", 0.9)) newActionString = "Destilling Moonshine";
        // else if (text.Contains("kill")) actionString = "Shootout"; // this can also be hunting
        else if 
            ((text.FuzzyContains("You killed", 0.9)
            || text.FuzzyContains("You downed", 0.9) || text.FuzzyContains("pressed charges against you", 0.9)
            || text.FuzzyContains("PARLEY", 0.9) || text.FuzzyContains("PRESS CHARGES", 0.9))
             && !text.FuzzyContains("Legendary", 0.9) && !text.FuzzyContains("Wait for ", 0.9)
            ) newActionString = "Gunfighting";
        if (newActionString != null) actionTime = Environment.TickCount64;
        if (newActionString != null) actionString = newActionString;
    }

    // Bounty: $4.05
    private void ParseBounty(string text)
    {
        const string needle = "Bounty: $";
        int index = text.IndexOf(needle);
        if (index >= 0)
        {
            int from = index + needle.Length;
            int to = from + 1;
            while (to < text.Length && !char.IsWhiteSpace(text[to])) to++;
            bounty = text.Substring(from, to - from);
        }
    }

    private static string ParseCrime(string text)
    {
        (int from, int length) index = text.FuzzyIndexOf("Investigating", 0.8);
        if (index.from < 0) index = text.FuzzyIndexOf("Witness", 0.8);
        if (index.from < 0) index = text.FuzzyIndexOf("Wanted", 0.8);
        if (index.from < 0) return null;
        int from = index.from + index.length;
        while (from < text.Length && char.IsWhiteSpace(text[from])) from++;
        int to = from + 1;
        while (to < text.Length && !char.IsWhiteSpace(text[to])) to++;
        return text.Substring(from, to - from).ToLower().Capitalize();
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create(location.Get(), GetAction() ?? "Roaming");
    }
}
