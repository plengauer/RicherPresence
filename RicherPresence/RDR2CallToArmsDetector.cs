using Discord;
public class RDR2CallToArmsDetector : RDR2ActivityDetector
{
    private int wave = 0;
    private bool fighting = false;
    private RDR2Location location = null;
    private long start = 0;

    public override bool IsActive()
    {
        return wave > 0;
    }

    public override void Parse(string text)
    {
        // do we want a state for overrun / defended?
        const string waveNeedle = "WAVE ";
        if (IsActive() && Environment.TickCount64 - start > 1000 * 60 * 60 * 2)
        {
            wave = -1;
            fighting = false;
            location = null;
        }
        else if (IsActive() && fighting && (text.FuzzyContains("WAVE COMPLETE", 0.9) || text.FuzzyContains("Prepare yourself and your allies for the next wave", 0.8)))
        {
            fighting = false;
            wave++;
        }
        else if (IsActive() && !fighting && text.FuzzyContains("Eliminate the enemies", 0.9))
        {
            fighting = true;
        }
        else if (IsActive() && (
            text.FuzzyContains("MISSION OVER", 0.8) /* || (text.FuzzyContains("COMPLETE", 0.9) && !text.FuzzyContains("WAVE COMPLETE", 0.8)) */ // this is getting too many false positives
            || (text.FuzzyContains("CALL TO ARMS COMPLETE", 0.9) && location.Get() != null && text.FuzzyContains(location.Get().ToUpper() + " DEFENDED", 0.9)
                && !text.FuzzyContains("CALL TO ARMS: " + location.Get().ToUpper(), 0.8) && !text.FuzzyContains("Defend " + location.Get() + " from the attackers", 0.8)
            )
            || text.FuzzyContains("SCOREBOARD", 0.8) || text.FuzzyContains("PROCEED", 0.8) || text.FuzzyContains("VOTE LIKE", 0.8) || text.FuzzyContains("VOTE DISLIKE", 0.8)
            || text.FuzzyContains("ABANDON ALONE", 0.8) || text.FuzzyContains("ABANDON WITH POSSE", 0.8) || text.FuzzyContains("REPLAY MISSION", 0.8)
        ))
        {
            wave = 0;
            fighting = false;
            location = null;
        }
        else if (IsActive() && text.FuzzyContains(waveNeedle, 0.95))
        {
            (int from, int length) index = text.FuzzyIndexOf(waveNeedle, 0.95);
            if (index.from < 0) return;
            int from = index.from + index.length;
            while (from < text.Length && char.IsWhiteSpace(text[from])) from++;
            int to = from;
            while (to < text.Length && char.IsDigit(text[to]) && to < from + 2) to++;
            if (to - from == 0 || to + from >= text.Length) return;
            wave = int.Parse(text.Substring(from, to - from));
            fighting = true;
        }
        else if (!IsActive() && text.FuzzyContains("CALL TO ARMS", 0.9) && text.FuzzyContains("PREPARE FOR AN ATTACK", 0.9))
        {
            wave = 1;
            fighting = false;
            start = Environment.TickCount64;
        }
        else if (text.FuzzyContains("CALL TO ARMS: ", 0.8) && text.Contains(":")) // && not call to arms more than once
        {
            (int from, int length) index = text.FuzzyIndexOf("CALL TO ARMS: ", 0.8);
            if (index.from < 0) return;
            if (!text.Substring(index.from, index.length).Contains(":")) return;
            if (text.Substring(index.from + index.length).FuzzyContains("CALL TO ARMS: ", 0.8)) return; // its there a second time? most likely we are in the menu
            int from = index.from + index.length;
            if (from >= text.Length) return;
            int to = from + 1;
            while (to < text.Length && text[to] != '\n') to++;
            if (!IsActive())
            {
                location = new RDR2Location();
                wave = 1;
                fighting = false;
                start = Environment.TickCount64;
            }
            location.Parse(text.Substring(from, to - from), RDR2Location.Type.LOCATION);
        }
        else
        {
            // CALL TO ARMS
            // Fort Mercer
            // Defend Fort Mercer from the attackers
            string[] lines = text.Split("\n");
            int index0 = -1;
            for (int i = 0; i < lines.Length && index0 < 0; i++) if (lines[i].FuzzyEquals("CALL TO ARMS", 0.9)) index0 = i;
            int index1 = index0 + 1;
            while (index1 < lines.Length && lines[index1].Length == 0) index1++;
            int index2 = index1 + 1;
            while (index2 < lines.Length && lines[index2].Length == 0) index2++;
            if (index0 < 0 || index1 < 0 || index2 < 0) return;
            (int index, int length) left = lines[index2].FuzzyIndexOf("Defend ", 0.9);
            (int index, int length) right = lines[index2].FuzzyIndexOf(" from the attackers", 0.9);
            if (left.index < 0 || right.index < 0) return;
            if (!IsActive())
            {
                location = new RDR2Location();
                wave = 1;
                fighting = false;
                start = Environment.TickCount64;
            }
            location.Parse(lines[index1], RDR2Location.Type.LOCATION);
            location.Parse(lines[index2].Substring(left.index + left.length, right.index - (left.index + left.length)), RDR2Location.Type.LOCATION);
        }
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create(
            "Call to Arms" + (location != null && location.Get() != null ? ": " + location.Get() : ""),
            (fighting ? "Defending against" : "Preparing for") + " Wave " + wave
        );
    }
}
