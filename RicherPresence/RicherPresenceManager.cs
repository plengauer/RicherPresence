using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

public abstract class RicherPresenceManager : RichPresenceManager
{

    private const int MAX_QUEUE_LENGTH = 60 * 5;

    private string name;
    private Screen screen;
    private OCR ocr;
    private int sleepTime;
    private bool deleteCaptures;

    private long nextID;
    private ActivityContext? nextContext;
    private object monitor;
    private BlockingQueue<Item> queueCaptures;
    private BlockingRevisionedQueue<Item> queueOCRs;
    private BlockingQueue<Item> queueActivities;

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

    public RicherPresenceManager(string name, string executable, Screen screen, OCR ocr, int sleepTime, bool limitQueues = true, bool deleteCaptures = true) : base(executable)
    {
        this.name = name;
        this.screen = screen;
        this.ocr = ocr;
        this.sleepTime = sleepTime;
        this.deleteCaptures = deleteCaptures;

        nextID = 0;
        nextContext = new ActivityContext();
        monitor = new object();
        queueCaptures = new BlockingQueue<Item>(limitQueues ? MAX_QUEUE_LENGTH : 0);
        queueOCRs = new BlockingRevisionedQueue<Item>(limitQueues ? MAX_QUEUE_LENGTH : 0, item => item.Id, 1000 * 60, nextID);
        queueActivities = new BlockingQueue<Item>(limitQueues ? MAX_QUEUE_LENGTH : 0);

        threadTrigger = threadParse = threadUpdate = null;
        threadsOCR = threadsCapture = null;

        activities = new ActivitySource(Observability.ACTIVITY_SOURCE_NAME);
        meter = new Meter(Observability.METER_SOURCE_NAME, "1.0.0");

        meter.CreateObservableGauge<int>("queue.length", () => new Measurement<int>[] {
            new Measurement<int>(  queueCaptures.Count, new KeyValuePair<string, object?>("discord.activity.name", name), new KeyValuePair<string, object?>("queue.name",   "captures")),
            new Measurement<int>(      queueOCRs.Count, new KeyValuePair<string, object?>("discord.activity.name", name), new KeyValuePair<string, object?>("queue.name",       "ocrs")),
            new Measurement<int>(queueActivities.Count, new KeyValuePair<string, object?>("discord.activity.name", name), new KeyValuePair<string, object?>("queue.name", "activities"))
        });
    }

    public override void Dispose()
    {
        base.Dispose();
        meter.Dispose();
        activities.Dispose();
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

        threadTrigger.Start();
        threadsCapture.ForEach(t => t?.Start());
        threadsOCR.ForEach(t => t?.Start());
        threadParse.Start();
        threadUpdate.Start();
    }

    protected override void Stop(IRichPresence presence)
    {
        // working theory: OCRs are all done, because queue is filled up very fast, second queue is killed because interrupt also interurpts when somebody is waiting for a lock
        // in theory we can jsut interrupt all in any order, dont care about what is still in the queue
        // however, since the tests work on simulated data, we have to make sure all is processed correctly
        // ATTENTION: interrupt will not just interrupt waits, but also monitor enters
        // this is why we wait for the queues to stabilize and then slowly shut everything down
        threadTrigger?.Interrupt();
        threadsCapture?.ForEach(t => t?.Interrupt());
        threadTrigger?.Join();
        threadsCapture?.ForEach(t => t?.Join());

        int revision = 0;
        while (revision != (revision = queueCaptures.Revision + queueOCRs.Revision + queueActivities.Revision)) Thread.Sleep(1000 * 10);

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
            try
            {
                int duration = (int)(Environment.TickCount64 - time);
                Thread.Sleep(Math.Max(1, Math.Min(sleepTime, sleepTime - duration)));
                time = Environment.TickCount64;
                using var root = activities.StartActivity("discord.richer_presence", ActivityKind.Server);
                root?.AddTag("discord.activity.name", name);
                lock (monitor)
                {
                    nextContext = root?.Context;
                    Monitor.PulseAll(monitor); // in theory can wake all, but only one is necessary
                }
            }
            catch (ThreadInterruptedException)
            {
                break;
            }
        }
    }

    private void RunCapture()
    {
        for (;;)
        {
            try
            {
                long myID;
                ActivityContext? myContext;
                lock (monitor)
                {
                    for (;;)
                    {
                        // wait for a pulse, race for the ID, and if win, continue, if not, wait for the next pulse
                        long myNextID = nextID;
                        Monitor.Wait(monitor);
                        if (myNextID == nextID) break;
                        else continue;
                    }
                    myID = nextID++;
                    myContext = nextContext;
                }
                using var span = activities.StartActivity("discord.richer_presence.capture_screen", ActivityKind.Internal, myContext ?? new ActivityContext());
                try
                {
                    string? screenshot = screen.Capture(myID);
                    if (screenshot == null) continue;
                    if (!queueCaptures.Enqueue(new Item { Id = myID, screenshot = screenshot, context = myContext })) File.Delete(screenshot);
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

    private void RunOCR()
    {
        for (;;)
        {
            try
            {
                Item item = queueCaptures.Dequeue();
                if (item.screenshot == null) continue;
                using var span = activities.StartActivity("discord.richer_presence.ocr", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    item.text = ocr.Parse(item.screenshot);
                    if (item.text == null) continue;
                    queueOCRs.Enqueue(item);
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
                using var span = activities.StartActivity("discord.richer_presence.parse", ActivityKind.Internal, item.context ?? new ActivityContext());
                try
                {
                    item.activity = ParseActivity(item.text);
                    if (!item.activity.HasValue || Equals(item.activity.Value, activity)) continue;
                    queueActivities.Enqueue(item);
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
    protected abstract Discord.Activity? ParseActivity(string text);

    private void RunUpdate(IRichPresence presence)
    {
        presence.Update(CreateInitialActivity());
        for (;;)
        {
            try
            {
                Item item = queueActivities.Dequeue();
                if (item.activity == null) continue;
                using var span = activities.StartActivity("discord.richer_presence.update", ActivityKind.Internal, item.context ?? new ActivityContext());
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

    protected abstract Discord.Activity CreateInitialActivity();

    private static bool Equals(Discord.Activity a1, Discord.Activity a2)
    {
        return (a1.Name == a2.Name) && (a1.Details == a2.Details) && (a1.State == a2.State);
    }
}

