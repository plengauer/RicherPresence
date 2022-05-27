using Discord;

public class RDR2TraderDeliveryDetector : RDR2ActivityDetector
{

    private bool active;
    private RDR2Location? destination;
    private bool griefed;
    private bool driving;

    public override bool IsActive()
    {
        return active;
    }

    public override void Parse(string text)
    {
        if (!active && (text.FuzzyContains("GOODS DELIVERY", 0.9) || text.FuzzyContains("SELL THE GOODS", 0.9) || text.FuzzyContains("Deliver the goods to", 0.9) || text.FuzzyContains("Protect the goods on the way to", 0.9)))
        {
            active = true;
            destination = new RDR2Location();
            griefed = false;
            driving = false;
        }
        else if (active && text.FuzzyContains("TRYING TO STEAL YOUR GOODS", 0.8))
        // else if (active && text.Contains("steal") && !text.Contains("rival") && !text.Contains("can") && !text.Contains("now"))
        {
            griefed = true;
        }
        else if (active && text.FuzzyContains("Protect the goods on the way to ", 0.8)) // protect the goods on the way to
        {
            driving = false;
            (int from, int length) index = text.FuzzyIndexOf("Protect the goods on the way to ", 0.8);
            if (index.from >= 0)
            {
                int from = index.from + index.length;
                int to = from;
                while (to < text.Length && text[to] != '\n') to++;
                destination.Parse(text.Substring(from, to - from), null);
            }
        }
        else if (active && text.FuzzyContains("Deliver the goods to ", 0.8)) // deliver the goods on the way to
        {
            driving = true;
            (int from, int length) index = text.FuzzyIndexOf("Deliver the goods to ", 0.8);
            if (index.from >= 0)
            {
                int from = index.from + index.length;
                int to = from;
                while (to < text.Length && text[to] != '\n') to++;
                destination.Parse(text.Substring(from, to - from), null);
            }
        }
        else if (active && (text.FuzzyContains("MISSION PASSED", 0.8) || text.FuzzyContains("MISSION FAILED", 0.8) || text.FuzzyContains("GOODS WERE SOLD", 0.8) || text.FuzzyContains("GOODS WERE STOLEN", 0.8) || text.FuzzyContains("GOODS WERE DESTROYED", 0.8)))
        {
            active = false;
            destination = null;
            griefed = false;
            driving = false;
        }
    }

    public override Activity Create()
    {
        return RDR2ActivityFactory.Create("Goods Delivery" + (destination != null && destination.Get() != null ? " to " + destination.Get() : ""), griefed ? "Defending" : (driving ? "Driving" : "Escorting"));
    }
 }


