using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

public class RDR2RichPresenceManager : RichPresenceManager
{

    private const long CLIENT_ID = 947170931015565323;
    private const uint STEAM_APPLICATION_ID = 1174180;
    private const int MAX_QUEUE_LENGTH = 60 * 5;

    private Screen screen;
    private OCR ocr;
    private int sleepTime;
    private bool blockWhenFull;
    private bool deleteCaptures;

    private long nextID;
    ActivityContext? nextContext;
    private object monitor;
    private BlockingQueue<Item> queueCaptures;
    private BlockingRevisionedQueue<Item> queueOCRs;
    private BlockingRevisionedQueue<Item> queueActivities;

    private struct Item
    {
        public long Id;
        public ActivityContext? context;
        public string? screenshot;
        public string? text;
        public Discord.Activity? activity;
    }

    private Thread? threadTrigger;
    private List<Thread?>? threadsCapture;
    private List<Thread?>? threadsOCR;
    private Thread? threadParse;
    private Thread? threadUpdate;

    private ActivitySource activities;
    private Meter meter;

    private RDR2InfoParser location;
    private RDR2ActivityDetector[] detectors;

    //TODO split into a RicherPresenceManager and an extending RDR2RicherPresenceManager, as a second step maybe not inherit but use a strategy?
    //TODO make some detector base classes, like for showdowns, with lobby screen, and so on
    // for example we can check lobby screen, and if that is gone for several seconds, lets assume it started
    // also, try to parse the timer
    public RDR2RichPresenceManager(Screen screen, OCR ocr, int sleepTime, bool blockWhenFull = false, bool deleteCaptures = true) : base("RDR2")
    {
        this.screen = screen;
        this.ocr = ocr;
        this.sleepTime = sleepTime;
        this.blockWhenFull = blockWhenFull;
        this.deleteCaptures = deleteCaptures;

        nextID = 0;
        nextContext = new ActivityContext();
        monitor = new object();
        queueCaptures = new BlockingQueue<Item>(MAX_QUEUE_LENGTH);
        queueOCRs = new BlockingRevisionedQueue<Item>(MAX_QUEUE_LENGTH, item => item.Id, 1000 * 60, nextID);
        queueActivities = new BlockingRevisionedQueue<Item>(MAX_QUEUE_LENGTH, item => item.Id, 1000 * 10, nextID);

        threadTrigger = threadParse = threadUpdate = null;
        threadsOCR = threadsCapture = null;

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
        base.Start(presence);
        
        threadTrigger = new Thread(() => RunTrigger()) { Name = GetType().Name + " Trigger" };
        threadsCapture = new List<Thread?>();
        for (int i = 0; i < 60 * 2; i++) threadsCapture.Add(new Thread(() => RunCapture()) { Name = GetType().Name + " Capture " + i });
        threadsOCR = new List<Thread?>();
        for (int i = 0; i < Environment.ProcessorCount; i++) threadsOCR.Add(new Thread(() => RunOCR()) { Name = GetType().Name + " OCR " + i });
        threadParse = new Thread(() => RunParse()) { Name = GetType().Name + " Parse" };
        threadUpdate = new Thread(() => RunUpdate(presence)) { Name = GetType().Name + " Update" };

        nextID = 0;
        nextContext = new ActivityContext();
        queueCaptures.Clear();
        queueOCRs.Reset(nextID);
        queueActivities.Reset(nextID);

        threadTrigger.Start();
        threadsCapture.ForEach(t => t?.Start());
        threadsOCR.ForEach(t => t?.Start());
        threadParse.Start();
        threadUpdate.Start();
    }

    protected override void Stop(IRichPresence presence)
    {
        threadTrigger?.Interrupt();
        threadTrigger?.Join();
        threadsCapture?.ForEach(t => t?.Interrupt());
        threadsCapture?.ForEach(t => t?.Join());
        queueCaptures.WaitForEmpty();
        threadsOCR?.ForEach(t => t?.Interrupt());
        threadsOCR?.ForEach(t => t?.Join());
        queueOCRs.WaitForEmpty();
        threadParse?.Interrupt();
        threadParse?.Join();
        queueActivities.WaitForEmpty();
        threadUpdate?.Interrupt();
        threadUpdate?.Join();

        queueCaptures.Clear();
        queueOCRs.Clear();
        queueActivities.Clear();

        threadsCapture = null;
        threadsOCR = null;
        threadParse = null;
        threadUpdate = null;

        base.Stop(presence);
    }

    private void RunTrigger()
    {
        long time = Environment.TickCount64;
        for (;;)
        {
            int duration = (int) (Environment.TickCount64 - time);
            try
            {
                Thread.Sleep(Math.Max(1, Math.Min(sleepTime, sleepTime - duration)));
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
            using var root = activities.StartActivity("discord.rich_presence.rdr2", ActivityKind.Server);
            time = Environment.TickCount64;
            lock (monitor)
            {
                nextContext = root?.Context;
                Monitor.Pulse(monitor); // in theory can wake all, but only one is necessary
            }
        }
    }

    private void RunCapture()
    {
        for (;;)
        {
            long myID;
            ActivityContext? myContext;
            lock (monitor)
            {
                for (;;)
                {
                    // wait for a pulse, race for the ID, and if win, continue, if not, wait for the next pulse
                    long myNextID = nextID;
                    try
                    {
                        Monitor.Wait(monitor);
                    }
                    catch (ThreadInterruptedException)
                    {
                        return;
                    }
                    if (myNextID == nextID) break;
                    else continue;
                }
                myID = nextID++;
                myContext = nextContext;
            }

            using var span = activities.StartActivity("discord.rich_presence.rdr2.capture_screen", ActivityKind.Internal, myContext ?? new ActivityContext());
            try
            {
                string? screenshot = screen.Capture(myID);
                if (screenshot == null) continue;
                if (!queueCaptures.Enqueue(new Item { Id = myID, screenshot = screenshot, context = myContext }, blockWhenFull)) File.Delete(screenshot);
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
    }

    private void RunOCR()
    {
        for (;;)
        {
            try
            {
                Item item = queueCaptures.Dequeue();
                if (item.screenshot == null) continue;
                using var span = activities.StartActivity("discord.rich_presence.rdr2.ocr", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    item.text = ocr.Parse(item.screenshot);
                    if (item.text == null) continue;
                    queueOCRs.Enqueue(item, blockWhenFull);
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
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    private void RunParse()
    {
        Discord.Activity activity = RDR2ActivityFactory.Create(null, null);
        for (;;)
        {
            try
            {
                Item item = queueOCRs.Dequeue();
                if (item.text == null) continue;
                using var span = activities.StartActivity("discord.rich_presence.rdr2.parse", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    item.activity = ParseActivity(item.text);
                    if (!item.activity.HasValue || Equals(item.activity.Value, activity)) continue;
                    queueActivities.Enqueue(item, blockWhenFull);
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
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    private void RunUpdate(IRichPresence presence)
    {
        presence.Update(RDR2ActivityFactory.Create(null, null));
        for (;;)
        {
            try
            {
                Item item = queueActivities.Dequeue();
                if (item.activity == null) continue;
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
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
        presence.Clear();
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

