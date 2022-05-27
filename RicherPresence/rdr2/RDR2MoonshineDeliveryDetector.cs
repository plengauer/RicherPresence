using Discord;

public class RDR2MoonshineDeliveryDetector : RDR2ActivityDetector
{
    private bool active;
    private RDR2Location? destination;
    private bool driving;

    public override bool IsActive()
    {
        return active;
    }

    public override void Parse(string text)
    {
        if (!active && (text.FuzzyContains("MOONSHINE DELIVERY", 0.9) || text.FuzzyContains("SELL THE MOONSHINE", 0.9) || text.FuzzyContains("Deliver the mooshine to", 0.9) || text.FuzzyContains("Protect the wagon on the way to", 0.9)))
        {
            active = true;
            destination = new RDR2Location();
            driving = false;
        }
        else if (active && text.FuzzyContains("Protect the wagon on the way to ", 0.8)) // protect the goods on the way to
        {
            driving = false;
            (int from, int length) index = text.FuzzyIndexOf("Protect the wagon on the way to ", 0.8);
            if (index.from >= 0)
            {
                int from = index.from + index.length;
                int to = from;
                while (to < text.Length && text[to] != '\n') to++;
                destination.Parse(text.Substring(from, to - from), RDR2Location.Type.LOCATION);
            }
        }
        else if (active && text.FuzzyContains("Deliver the moonshine to ", 0.8)) // deliver the goods on the way to
        {
            driving = true;
            (int from, int length) index = text.FuzzyIndexOf("Deliver the moonshine to ", 0.8);
            if (index.from >= 0)
            {
                int from = index.from + index.length;
                int to = from;
                while (to < text.Length && text[to] != '\n') to++;
                destination.Parse(text.Substring(from, to - from), RDR2Location.Type.LOCATION);
            }
        }
        else if (active && (text.FuzzyContains("MISSION PASSED", 0.8) || text.FuzzyContains("MISSION FAILED", 0.8) || text.FuzzyContains("MOONSHINE WAS SOLD", 0.8) || text.FuzzyContains("MOONSHINE WAS DESTROYED", 0.8)))
        {
            active = false;
            destination = null;
            driving = false;
        }
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create("Moonshine Delivery" + (destination != null && destination.Get() != null ? " to " + destination.Get() : ""), driving ? "Driving" : "Escorting");
    }

}