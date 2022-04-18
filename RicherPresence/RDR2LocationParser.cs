using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RDR2LocationParser : RDR2InfoParser
{

    private static ActivitySource ACTIVITIES = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);

    private RDR2Location location = new RDR2Location(), area = new RDR2Location(), state = new RDR2Location();

    public override string? Get()
    {
        string? l = location.Get(), a = area.Get(), s = state.Get();
        return s != null ? (l != null ? l + ", " : "") + (a != null ? a + ", " : "") + s : null;
    }

    // Rhodes
    // Scarlet Meadows, LE
    // 4:15PM | 25C
    public override void Parse(string text)
    {
        string[] lines = text.Split('\n');
        int index = 0;
        while (index < lines.Length && !(lines[index].FuzzyContains("AM", 0.99) || lines[index].FuzzyContains("PM", 0.99) || (lines[index].Contains(":") && lines[index].Contains("|") && lines[index].Contains("C")))) index++;
        if (index >= lines.Length) return;

        using var s = ACTIVITIES.StartActivity("discord.rich_presence.rdr2.parse_full_location");
        s?.AddTag("rdr2.location.input", text);
        int line1 = index - 1;
        while (line1 >= 0 && lines[line1].Trim().Length == 0) line1--;
        if (line1 < 0) return;
        int line0 = line1 - 1;
        while (line0 >= 0 && lines[line0].Trim().Length == 0) line0--;
        if (line0 < 0) return;

        string? location, area, state;
        if (lines[line1].Contains(","))
        {
            location = lines[line0];
            area = lines[line1].Substring(0, lines[line1].IndexOf(','));
            state = lines[line1].Substring(lines[line1].IndexOf(',') + 1);
        }
        else
        {
            location = null;
            area = lines[line0];
            state = lines[line1];
        }

        RDR2Location locationV1 = new RDR2Location(), areaV1 = new RDR2Location(), stateV1 = new RDR2Location();

        double scoreLocation = locationV1.Parse(location, RDR2Location.Type.LOCATION);
        double scoreArea = areaV1.Parse(area, RDR2Location.Type.AREA);
        double scoreState = stateV1.Parse(state, RDR2Location.Type.STATE);

        if (scoreLocation < 0 && scoreArea < 0 && scoreState < 0) return;

        string? hintLocation = null, hintArea = null, hintState = null;

        if (scoreLocation >= Math.Max(scoreArea, scoreState)) hintLocation = locationV1.Get();
        else if (scoreArea >= Math.Max(scoreLocation, scoreState)) hintArea = areaV1.Get();
        else if (scoreState >= Math.Max(scoreArea, scoreLocation)) hintState = stateV1.Get();

        this.location.Reset();
        this.area.Reset();
        this.state.Reset();

        this.location.Parse(location, RDR2Location.Type.LOCATION, hintState, hintArea, hintLocation);
        this.area.Parse(area, RDR2Location.Type.AREA, hintState, hintArea, hintLocation);
        this.state.Parse(state, RDR2Location.Type.STATE, hintState, hintArea, hintLocation);
        
        s?.AddTag("rdr2.location.output.state", this.state.Get());
        s?.AddTag("rdr2.location.output.area", this.area.Get());
        s?.AddTag("rdr2.location.output.location", this.location.Get());

        Debug.Assert(RDR2Location.IsValid(this.state.Get(), this.area.Get(), this.location.Get()), text + " => " + Get());
    }
}
