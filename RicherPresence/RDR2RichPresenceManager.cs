using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

//TODO if we need more speed, to some parallel OCR'ing
//TODO capture individual parts to improve accuracy, ffmpeg can crop on the fly
//TODO check whether ffmpeg and tesseract can handle several ones at a time

public class RDR2RichPresenceManager : RichPresenceManager
{

    private const long CLIENT_ID = 947170931015565323;
    private const uint STEAM_APPLICATION_ID = 1174180;
    private const uint MAX_QUEUE_LENGTH = 60 * 5;

    private Screen screen;
    private OCR ocr;
    private int sleepTime;
    private bool waitOnOverflow;
    private bool deleteCaptures;

    private ConcurrentQueue<Item> queueCaptures = new ConcurrentQueue<Item>();
    private ConcurrentQueue<Item> queueOCRs = new ConcurrentQueue<Item>();
    private ConcurrentQueue<Item> queueActivities = new ConcurrentQueue<Item>();

    private struct Item
    {
        public ActivityContext? context;
        public string? screenshot;
        public string? text;
        public Discord.Activity? activity;
    }

    private Thread? threadCapture;
    private Thread? threadOCR;
    private Thread? threadParse;
    private Thread? threadUpdate;
    private volatile bool active;

    private ActivitySource activities;
    private Meter meter;

    private RDR2InfoParser location;
    private RDR2ActivityDetector[] detectors;

    public RDR2RichPresenceManager(Screen screen, OCR ocr, int sleepTime, bool waitOnOverflow = true, bool deleteCaptures = true) : base("RDR2")
    {
        this.screen = screen;
        this.ocr = ocr;
        this.sleepTime = sleepTime;
        this.waitOnOverflow = waitOnOverflow;
        this.deleteCaptures = deleteCaptures;

        queueCaptures = new ConcurrentQueue<Item>();
        queueOCRs = new ConcurrentQueue<Item>();
        queueActivities = new ConcurrentQueue<Item>();

        threadCapture = threadOCR = threadParse = threadUpdate = null;
        active = false;

        activities = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);
        meter = new Meter(Observability.METER_SOURCE_NAME, "1.0.0");

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
            new RDR2GenericMissionDetector(new string[] { "search", "for the valuables" }, new string[] { "ROBBERY SUCCESSFULL", "HOMESTEAD ROBBED" }, 1000 * 60 * 10, _ => location.Get(), _ => "Robbing a Homestead"),
            // bloodmoney opportunity
            //TODO
            // posse activities
            new RDR2GenericMissionDetector(new string[] { "INFIGHTING" }, new string[] { "INFIGHTING WON", "INFIGHTING LOST" }, 1000 * 60 * 10, _ => location.Get(), _ => "Infighting"),
            // showdowns
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "HEAD FOR THE HILLS" },   new string[] { }, 1000 * 60 * (10 * 2), _ => "Head for the Hills", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "PUBLIC ENEMY" },         new string[] { }, 1000 * 60 * (10 * 2), _ => "Public Enemy", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "SPORT OF KINGS" },       new string[] { }, 1000 * 60 * (10 * 2), _ => "Sport of Kings", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "OVERRUN" },              new string[] { }, 1000 * 60 * (10 * 2), _ => "Overrun", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "PLUNDER" },              new string[] { }, 1000 * 60 * (10 * 2), _ => "Plunder", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "SPOILS OF WAR" },        new string[] { }, 1000 * 60 * (10 * 2), _ => "Spoils of War", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "UP IN SMOKE" },          new string[] { }, 1000 * 60 * (10 * 2), _ => "Up in Smoke", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "SHOOTOUT" },             new string[] { }, 1000 * 60 * (10 * 2), _ => "Shootout", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "GUN RUSH" },             new string[] { }, 1000 * 60 * (10 * 2), _ => "Gun Rush", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "HOSTILE TERRITORY" },    new string[] { }, 1000 * 60 * (10 * 2), _ => "Hostile Territory", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "NAME YOUR WEAPON" },     new string[] { }, 1000 * 60 * (10 * 2), _ => "Name Your Weapon", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "MOST WANTED" },          new string[] { }, 1000 * 60 * (10 * 2), _ => "Most Wanted", _ => null),
            new RDR2GenericMissionDetector(new string[] { "Players", "Game Details", "Purchases", "Quit to Free Roam", "MAKE IT COUNT" },        new string[] { }, 1000 * 60 * (10 * 2), _ => "Make It Count", _ => null),
            // races
            // TODO
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
            // TODO sale
            // naturalist activities
            new RDR2GenericMissionDetector(new string[] { "NATURALIST", "RETRIEVE A SAMPLE FROM THE LEGENDARY" }, new string[] { "SAMPLE RETRIEVED", "SKINNED" }, 1000 * 60 * 60, _ => location.Get(), _ => "Hunting a Legendary Animal"),
            // TODO poachers
            // free roam events
            new RDR2GenericMissionDetector(new string[] { "KING OF THE CASTLE", "CAPTURE & CONTROL THE AREAS" }, new string[] { "KING OF THE CASTLE OVER" }, 1000 * 60 * 10, _ => "King of the Castle", _ => null),
            new RDR2GenericMissionDetector(new string[] { "COLD DEAD HANDS", "HOLD A BAG" }, new string[] { "COLD DEAD HANDS OVER" }, 1000 * 60 * 5, _ => "Cold Dead Hands", text => {
                if (text.FuzzyContains("Pick up a bag", 0.9)) return "Competing";
                else if (text.FuzzyContains("Take a bag from ", 0.8)) return "Attacking";
                else if (text.FuzzyContains("Hold the bag", 0.8)) return "Defending";
                else return null;
            }),
            new RDR2GenericMissionDetector(new string[] { "RAILROAD BARON", "CAPTURE AND CONTROL THE TRAIN CAR" }, new string[] { "RAILROAD BARON OVER" }, 1000 * 60 * 10, _ => "Railroad Baron", text => {
                if (text.FuzzyContains("Gain control of the train car from ", 0.9)) return "Attacking";
                else if (text.FuzzyContains("Gain control of the train car ", 0.8)) return "Competing";
                else if (text.FuzzyContains("Defend the train car", 0.8) || text.FuzzyContains("Help defend the train car", 0.8)) return "Defending";
                else return null;
            }),
            new RDR2GenericMissionDetector(new string[] { "FOOL'S GOLD", "GET THE GOLDEN ARMOR AND TAKE OUT PLAYERS TO EARN POINTS" }, new string[] { "FOOL'S GOLD OVER" }, 1000 * 60 * 10, _ => "Fool's Gold", _ => null),
            new RDR2GenericMissionDetector(new string[] { "WILDLIFE PHOTOGRAPHER", "TAKE THE BEST PHOTOGRAPHS OF ANIMALS TO EARN POINTS" }, new string[] { "WILDLIFE PHOTOGRAPHER OVER", "PLACE" }, 1000 * 60 * 10, _ => "Wildlife Photographer", _ => null),
            new RDR2GenericMissionDetector(new string[] { "DISPATCH RIDER", "DELIVER THE HORSE" }, new string[] { "DISPATCH RIDER OVER" }, 1000 * 60 * 10, _ => "Dispatch Rider", _ => null),
            new RDR2GenericMissionDetector(new string[] { "MASTER ARCHER", "SHOOT THE TARGETS" }, new string[] { "MASTER ARCHER OVER", "PLACE" }, 1000 * 60 * 10, _ => "Master Archer", _ => null),
            // free roam events (roles)
            new RDR2GenericMissionDetector(new string[] { "PROTECT LEGENDARY ANMIAL", "WORK TOGETHER AND RELEASE THE LEGENDARY" }, new string[] { "PROTECT LEGENDARY ANMIAL OVER" }, 1000 * 60 * 10, _ => "Protect Legendary Animal", _ => null),
            new RDR2GenericMissionDetector(new string[] { "DAY OF RECKONING", "CAPTURE BOUNTY TARGETS AND SCORE THE MOST POINTS" }, new string[] { "DAY OF RECKONING OVER", "PLACE" }, 1000 * 60 * 10, _ => "Day of Reckoning", _ => null),
            new RDR2GenericMissionDetector(new string[] { "ANIMAL TAGGING", "SEDATE AND TAG AS MANY ANIMALS AS POSSIBLE" }, new string[] { "ANIMAL TAGGING OVER", "PLACE" }, 1000 * 60 * 10, _ => "Animal Tagging", _ => null),
            new RDR2GenericMissionDetector(new string[] { "MAN HUNT", "WORK TOGETHER AND CAPTURE THE BOUNTY TARGETS" }, new string[] { "MAN HUNT OVER" }, 1000 * 60 * 10, _ => "Man Hunt", _ => null),
            new RDR2GenericMissionDetector(new string[] { "TRADE ROUTE", "WORK TOGETHER AND PROTECT THE GOODS" }, new string[] { "TRADE ROUTE OVER" }, 1000 * 60 * 15, _ => "Trade Route", _ => "Protecting the Train"),
            new RDR2GenericMissionDetector(new string[] { "CONDOR EGG", "FIND THE CONDOR EGG WORTH" }, new string[] { "CONDOR EGG OVER" }, 1000 * 60 * 10, _ => "Condor Egg", _ => null),
            new RDR2GenericMissionDetector(new string[] { "SALVAGE", "SEARCH THE COLLECTIBLES" }, new string[] { "SALVAGE OVER" }, 1000 * 60 * 10, _ => "Salvage", _ => null),
            new RDR2CallToArmsDetector(),
            new RDR2FreeRoamDetector(location)
        };

        meter.CreateObservableGauge<int>("queue.length", () => new Measurement<int>[] {
            new Measurement<int>(  queueCaptures.Count, new KeyValuePair<string, object?>("queue.name",   "captures")),
            new Measurement<int>(      queueOCRs.Count, new KeyValuePair<string, object?>("queue.name",       "ocrs")),
            new Measurement<int>(queueActivities.Count, new KeyValuePair<string, object?>("queue.name", "activities"))
        });
    }

    public override void Dispose()
    {
        base.Dispose();
        meter.Dispose();
        activities.Dispose();
    }

    protected override IRichPresence CreateRichPresence()
    {
        return new RichPresence(CLIENT_ID, STEAM_APPLICATION_ID);
    }

    protected override void Start(IRichPresence presence)
    {
        threadCapture = new Thread(() => RunCapture());
        threadOCR = new Thread(() => RunOCR());
        threadParse = new Thread(() => RunParse());
        threadUpdate = new Thread(() => RunUpdate(presence));
        active = true;
        threadCapture.Start();
        threadOCR.Start();
        threadParse.Start();
        threadUpdate.Start();
    }

    protected override void Stop(IRichPresence presence)
    {
        active = false;
        threadCapture?.Join();
        threadOCR?.Join();
        threadParse?.Join();
        threadUpdate?.Join();
        queueCaptures.Clear();
        queueOCRs.Clear();
        queueActivities.Clear();
        threadCapture = null;
        threadOCR = null;
        threadParse = null;
        threadUpdate = null;
    }

    private bool Enqueue(ConcurrentQueue<Item> queue, Item item)
    {
        if (waitOnOverflow)
        {
            while (queue.Count >= MAX_QUEUE_LENGTH) Thread.Sleep(Math.Max(1, sleepTime / 100));
            queue.Enqueue(item);
            return true;
        }
        else if (queue.Count < MAX_QUEUE_LENGTH)
        {
            queue.Enqueue(item);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void RunCapture()
    {
        int time = Environment.TickCount;
        while (active)
        {
            int duration = (Environment.TickCount - time);
            Thread.Sleep(Math.Max(1, Math.Min(sleepTime, sleepTime - duration)));
            using var root = activities.StartActivity("discord.rich_presence.rdr2", ActivityKind.Server);
            time = Environment.TickCount;
            using var span = activities.StartActivity("discord.rich_presence.rdr2.capture_screen");
            if (screen.IsDone()) return;
            string screenshot = screen.Capture();
            if (screenshot == null) continue;
            if (!Enqueue(queueCaptures, new Item { screenshot = screenshot, context = root?.Context })) File.Delete(screenshot);
        }
    }

    private void RunOCR()
    {
        while ((threadCapture != null && threadCapture.IsAlive) || queueCaptures.Count > 0)
        {
            Item item;
            if (queueCaptures.TryDequeue(out item) && item.screenshot != null)
            {
                using var span = activities.StartActivity("discord.rich_presence.rdr2.ocr", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    item.text = ocr.Parse(item.screenshot);
                    if (item.text == null) continue;
                    Enqueue(queueOCRs, item);
                }
                catch (Exception exception)
                {
                    var tags = new ActivityTagsCollection();
                    tags.Add("exception.type", exception.GetType().Name);
                    tags.Add("exception.message", exception.Message);
                    tags.Add("exception.stacktrace", exception.ToString());
                    span?.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
                }
                finally
                {
                    if (deleteCaptures) File.Delete(item.screenshot);
                }
            }
            else
            {
                Thread.Sleep(sleepTime);
            }
        }
    }

    private void RunParse()
    {
        Discord.Activity activity = RDR2ActivityFactory.Create(null, null);
        while ((threadOCR != null && threadOCR.IsAlive) || queueOCRs.Count > 0)
        {
            Item item;
            if (queueOCRs.TryDequeue(out item) && item.text != null)
            {
                using var span = activities.StartActivity("discord.rich_presence.rdr2.parse", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    item.activity = ParseActivity(item.text);
                    if (!item.activity.HasValue || Equals(item.activity.Value, activity)) continue;
                    Enqueue(queueActivities, item);
                    activity = item.activity.Value;
                }
                catch (Exception exception)
                {
                    var tags = new ActivityTagsCollection();
                    tags.Add("exception.type", exception.GetType().Name);
                    tags.Add("exception.message", exception.Message);
                    tags.Add("exception.stacktrace", exception.ToString());
                    span?.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
                }
            }
            else
            {
                Thread.Sleep(sleepTime);
            }
        }
    }

    private void RunUpdate(IRichPresence presence)
    {
        presence.Update(RDR2ActivityFactory.Create(null, null));
        while ((threadParse != null && threadParse.IsAlive) || queueActivities.Count > 0)
        {
            Item item;
            if (queueActivities.TryDequeue(out item) && item.activity != null)
            {
                using var span = activities.StartActivity("discord.rich_presence.rdr2.update", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    presence.Update(item.activity.Value);
                }
                catch (Exception exception)
                {
                    var tags = new ActivityTagsCollection();
                    tags.Add("exception.type", exception.GetType().Name);
                    tags.Add("exception.message", exception.Message);
                    tags.Add("exception.stacktrace", exception.ToString());
                    span?.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
                }
            }
            else
            {
                Thread.Sleep(sleepTime);
            }
        }
    }

    private static bool Equals(Discord.Activity a1, Discord.Activity a2)
    {
        return (a1.Name == a2.Name) && (a1.Details == a2.Details) && (a1.State == a2.State);
    }

    private Discord.Activity? ParseActivity(string text)
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

