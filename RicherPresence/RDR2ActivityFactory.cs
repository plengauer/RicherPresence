using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

public class RDR2ActivityFactory
{

    private const long APPLICATION_ID = 947170931015565323;

    public static Activity Create(string? details, string? state)
    {
        bool honorable = new Random().NextDouble() < 0.5;
        return new Activity()
        {
            ApplicationId = APPLICATION_ID,
            Name = "Red Dead Redemption 2",
            Details = details,
            State = state,
            Assets = new ActivityAssets
            {
                LargeImage = "rdr2",
                LargeText = "Red Dead Redemption 2",
                SmallImage = honorable ? "high_honor" : "low_honor",
                SmallText = honorable ? "High Honor" : "Low Honor"
            }
        };
    }

}

