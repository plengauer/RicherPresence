using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO IDEAS
// try parse timer
// save current active?
// globalize timeout handling?

public class RDR2RicherPresenceManager : RicherPresenceManager
{

    private const long CLIENT_ID = 947170931015565323;
    private const uint STEAM_APPLICATION_ID = 1174180;

    private RDR2InfoParser location;
    private RDR2ActivityDetector[] detectors;

    public RDR2RicherPresenceManager(ILoggerFactory factory, Screen screen, OCR ocr, int sleepTime, bool limitQueues = true, bool deleteCaptures = true) : base(factory, "Red Dead Redemption 2", "RDR2", screen, ocr, sleepTime, limitQueues, deleteCaptures)
    {
        location = new RDR2LocationParser();
        detectors = new RDR2ActivityDetector[]
        {
            // freeroam missions
            new RDR2GenericMissionDetector(new string[] { "CARAVAN ESCORT", "ESCORT THE CARAVAN TO THE DESTINATION" }, new string[] { "CARAVAN REACHED DESTINATION" }, 1000 * 60 * 15, _ => location.Get(), _ => "Escorting a Caravan"),
            new RDR2GenericMissionDetector(new string[] { "DELIVERY", "DELIVER THE BAG" }, new string[] { "BAG DELIVERED", "BAG STOLEN" }, 1000 * 60 * 15, _ => location.Get(), _ => "Deliverying Mail"),
            new RDR2GenericMissionDetector(new string[] { "ON THE HUNT", "HUNT & DELIVER THE" }, new string[] { "PROOF OF KILL DELIVERED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Hunting"),
            new RDR2GenericMissionDetector(new string[] { "STOLEN WAGON", "RECLAIM THE WAGON AND RETURN IT TO THE DROP OFF" }, new string[] { "THE WAGON WAS RETURNED", "THE WAGON WAS DESTROYED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Repossessing a Wagon"),
            new RDR2GenericMissionDetector(new string[] { "STOLEN BOAT", "RECLAIM THE BOAT AND RETURN IT TO THE DROP OFF" }, new string[] { "THE BOAT WAS RETURNED", "THE BOAT WAS DESTROYED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Repossessing a Wagon"),
            new RDR2GenericMissionDetector(new string[] { "RECOVERY", "FIND AND RECOVER THE LOST WAGON" }, new string[] { "WAGON DELIVERED", "WAGON DESTROYED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Recovering a Wagon"),
            new RDR2GenericMissionDetector(new string[] { "RECOVERY", "FIND AND RECOVER THE LOST CART" }, new string[] { "CART DELIVERED", "CART DESTROYED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Recovering a Cart"),
            new RDR2GenericMissionDetector(new string[] { "RECOVERY", "FIND AND RECOVER THE LOST BOAT" }, new string[] { "BOAT DELIVERED", "BOAT DESTROYED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Recovering a Cart"),
            // new RDR2GenericMissionDetector(new string[] { "RESCUE", "" }, new string[] { "" }, 1000 * 60 * 15, _ => location.Get(), _ => "Rescuing a Person in Need"),
            // new RDR2GenericMissionDetector(new string[] { "SUPPLY", "" }, new string[] { "" }, 1000 * 60 * 15, _ => location.Get(), _ => "Deliverying Supplies"),
            new RDR2GenericMissionDetector(new string[] { "EARLY RELEASE", "RESCUE THE PRISONERS FROM THE WAGON" }, new string[] { /* "PRISONERS FREED" */ }, 1000 * 60 * 15, _ => location.Get(), _ => "Breaking an Outlaw out of Jail"),
            new RDR2GenericMissionDetector(new string[] { "JAILBREAK", "BREAK THE PRISONER OUT OF JAIL" }, new string[] { "PRISONERS FREED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Breaking an Outlaw out of Jail"),
            new RDR2GenericMissionDetector(new string[] { "PAID KILLING", "FIND & ELIMINATE THE " }, new string[] { "TARGETS ELIMINATED", "TARGETS ESCAPED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Assassinating"),
            // new RDR2GenericMissionDetector(new string[] { "EARLY EXECUTION", "" }, new string[] { "" }, 1000 * 60 * 15, _ => location.Get(), _ => "Executing a Criminal"),
            new RDR2GenericMissionDetector(new string[] { "HORSE THEFT", "STEAL THE HORSE" }, new string[] { "HORSE DELIVERED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Stealing a Horse"),
            new RDR2GenericMissionDetector(new string[] { "WAGON THEFT", "STEAL THE WAGON" }, new string[] { "WAGON DELIVERED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Stealing a Wagon"),
            new RDR2GenericMissionDetector(new string[] { "BOAT THEFT", "STEAL THE BOAT" }, new string[] { "BOAT DELIVERED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Stealing a Boat"),
            new RDR2GenericMissionDetector(new string[] { "BUSHWACK", "INTERCEPT & DELIVER THE WAGON" }, new string[] { "WAGON DELIVERED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Bushwacking"),
            new RDR2GenericMissionDetector(new string[] { "DESTROY SUPPLIES", "DESTROY THE SUPPLIES" }, new string[] { "THE SUPPLIES WERE DESTROYED" }, 1000 * 60 * 15, _ => location.Get(), _ => "Destroying Supplies"),
            // bloodmoney missions
            // TODO
            new RDR2GenericMissionDetector(new string[] { "search", "for the valuables" }, new string[] { "ROBBERY SUCCESSFUL", "ROBBERY FAILED", "HOMESTEAD ROBBED" }, 1000 * 60 * 10, _ => location.Get(), _ => "Robbing a Homestead"),
            // bloodmoney opportunity
            //TODO
            // posse activities
            new RDR2GenericMissionDetector(new string[] { "INFIGHTING" }, new string[] { "INFIGHTING WON", "INFIGHTING LOST" }, 1000 * 60 * 10, _ => location.Get(), _ => "Infighting"),
            // showdowns
            new RDR2ShowdownDetector("Head for the Hills", _ => null),
            new RDR2ShowdownDetector("Public Enemy", _ => null),
            new RDR2ShowdownDetector("Sport of Kings",  _ => null),
            new RDR2ShowdownDetector("Overrun", _ => null),
            new RDR2ShowdownDetector("Plunder", _ => null),
            new RDR2ShowdownDetector("Spoils of War", _ => null),
            new RDR2ShowdownDetector("Up in Smoke", _ => null),
            new RDR2ShowdownDetector("Shootout", _ => null),
            new RDR2ShowdownDetector("Gun Rush", _ => null),
            new RDR2ShowdownDetector("Hostile Territory", _ => null),
            new RDR2ShowdownDetector("Name Your Weapon", _ => null),
            new RDR2ShowdownDetector("Most Wanted", _ => null),
            new RDR2ShowdownDetector("Make It Count", _ => null),
            // races
            // TODO
            // free roam events
            new RDR2FreeRoamEventDetector("King of the Castle", "CAPTURE & CONTROL THE AREAS", _ => null),
            new RDR2FreeRoamEventDetector("Cold Dead Hands", "HOLD A BAG", text => {
                if (text.FuzzyContains("Pick up a bag", 0.9)) return "Competing";
                else if (text.FuzzyContains("Take a bag from ", 0.8)) return "Attacking";
                else if (text.FuzzyContains("Hold the bag", 0.8)) return "Defending";
                else return null;
            }),
            new RDR2FreeRoamEventDetector("Railroad Baron", "CAPTURE AND CONTROL THE TRAIN CAR", text => {
                if (text.FuzzyContains("Gain control of the train car from ", 0.9)) return "Attacking";
                else if (text.FuzzyContains("Gain control of the train car ", 0.8)) return "Competing";
                else if (text.FuzzyContains("Defend the train car", 0.8) || text.FuzzyContains("Help defend the train car", 0.8)) return "Defending";
                else return null;
            }),
            new RDR2FreeRoamEventDetector("Fool's Gold", "GET THE GOLDEN ARMOR AND TAKE OUT PLAYERS TO EARN POINTS", _ => null),
            new RDR2FreeRoamEventDetector("Wildlife Photographer", "TAKE THE BEST PHOTOGRAPHS OF ANIMALS TO EARN POINTS", _ => null),
            new RDR2FreeRoamEventDetector("Dispatch Rider", "DELIVER THE HORSE", _ => null),
            new RDR2FreeRoamEventDetector("Master Archer", "SHOOT THE TARGETS", _ => null),
            // free roam events (roles)
            new RDR2FreeRoamEventDetector("Protect Legendary Animal", "WORK TOGETHER AND RELEASE THE LEGENDARY", _ => null),
            new RDR2FreeRoamEventDetector("Day of Reckoning", "CAPTURE BOUNTY TARGETS AND SCORE THE MOST POINTS", _ => null),
            new RDR2FreeRoamEventDetector("Animal Tagging", "SEDATE AND TAG AS MANY ANIMALS AS POSSIBLE", _ => null),
            new RDR2FreeRoamEventDetector("Man Hunt", "WORK TOGETHER AND CAPTURE THE BOUNTY TARGETS", _ => null),
            new RDR2FreeRoamEventDetector("Trade Route", "WORK TOGETHER AND PROTECT THE GOODS", _ => "Protecting the Baggage Train"),
            new RDR2FreeRoamEventDetector("Condor Egg", "FIND THE CONDOR EGG WORTH", _ => "Searching the Condor Egg"),
            new RDR2FreeRoamEventDetector("Salvage", "SEARCH THE COLLECTIBLES", _ => null),
            new RDR2CallToArmsDetector(),
            // bounty hunter activities
            new RDR2GenericMissionDetector(new string[] { "BOUNTY HUNTER", "CAPTURE" }, new string[] { "BOUNTY COMPLETE", "BOUNTY DELIVERED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Bounty Hunting"),
            new RDR2GenericMissionDetector(new string[] { "Kill the Bounty Hunters" }, new string[] { "HUNT OVER", "YOU SURVIVED" }, 1000 * 60 * 30, _ => location.Get(), _ => "Running from Bounty Hunters"),
            // TODO legendary bounties
            // trader activities
            new RDR2GenericMissionDetector(new string[] { "RESUPPLY", "GET THE SUPPLIES" }, new string[] { "DELIVERED" }, 1000 * 60 * 20, _ => location.Get(), _ => "Resupplying"),
            new RDR2TraderDeliveryDetector(),
            // moonshiner activities
            new RDR2GenericMissionDetector(new string[] { "MOONSHINER", "SABOTAGE THE MOONSHINER'S STILL" }, new string[] { "MASH INGREDIENTS COST REDUCED", "RIVAL STILL WAS SABOTAGED", "RIVAL STILL WAS DESTROYED" }, 1000 * 60 * 10, _ => location.Get(), _ => "Sabotaging a Rival Moonshine Still"),
            new RDR2GenericMissionDetector(new string[] { "MOONSHINER", "BRAWL WITH THE RIVAL MOONSHINERS" }, new string[] { "MASH INGREDIENTS COST REDUCED", "RIVAL MOONSHINERS WERE KNOCKED OUT" }, 1000 * 60 * 10, _ => location.Get(), _ => "Brawling with Rival Moonshiners"),
            new RDR2GenericMissionDetector(new string[] { "MOONSHINER", "FIND THE RIVAL CROP FIELDS" }, new string[] { "MASH INGREDIENTS COST REDUCED", "THE CROPS WERE DESTROYED" }, 1000 * 60 * 10, _ => location.Get(), _ => "Burning Crop Fields of Rival Moonshiners"),
            new RDR2GenericMissionDetector(new string[] { "MOONSHINER", "CLEAR OUT THE REVENUE AGENTS" }, new string[] { "MASH INGREDIENTS COST REDUCED", "ROADBLOCK CLEARED" }, 1000 * 60 * 10, _ => location.Get(), _ => "Clearing a Roadblock"),
            // TODO destroying supplies, escorting patron, rescuing buyer
            new RDR2MoonshineDeliveryDetector(),
            // naturalist activities
            new RDR2GenericMissionDetector(new string[] { "NATURALIST", "RETRIEVE A SAMPLE FROM THE LEGENDARY" }, new string[] { "SAMPLE RETRIEVED", "SKINNED" }, 1000 * 60 * 60, _ => location.Get(), _ => "Hunting a Legendary Animal"),
            // TODO poachers
            new RDR2FreeRoamDetector(location)
        };
    }

    protected override IRichPresence CreateRichPresence(ILoggerFactory factory)
    {
        return new RichPresence(factory, CLIENT_ID, STEAM_APPLICATION_ID);
    }

    protected override Discord.Activity CreateInitialActivity()
    {
        return RDR2ActivityFactory.Create(null, null);
    }

    protected override Discord.Activity? ParseActivity(string text)
    {
        //TODO maybe maintaining the current "active" detector is a necessary optimization
        if (text.Length < 5) return null;
        location.Parse(text);
        foreach (RDR2ActivityDetector detector in detectors)
        {
            detector.Parse(text);
            if (!detector.IsActive()) continue;
            return detector.Create();
        }
        return RDR2ActivityFactory.Create(null, null);
    }
}

