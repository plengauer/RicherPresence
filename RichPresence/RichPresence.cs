using System.Diagnostics;

public class RichPresence : IRichPresence
{
    private static readonly ActivitySource ActivitySource = new ActivitySource(IRichPresence.ACTIVITY_SOURCE_NAME);

    private Discord.Discord discord;
    private Thread thread;
    private bool active;

    public RichPresence(long clientID, uint steamID)
    {
        discord = new Discord.Discord(clientID, (ulong) Discord.CreateFlags.NoRequireDiscord);
        thread = new Thread(() => Run());
        active = true;

        // discord.SetLogHook(Discord.LogLevel.Debug, null /*(level, message) => Console.WriteLine(message)*/);
        discord.GetActivityManager().RegisterSteam(steamID);
        thread.Start();
    }

    public virtual void Clear()
    {
        using var span = ActivitySource.StartActivity("discord.rich_presence.clear");
        lock (discord)
        {
            Console.WriteLine(DateTime.Now + ": null");
            discord.GetActivityManager().ClearActivity(null /*result => { }*/);
        }
    }

    public virtual void Update(Discord.Activity activity)
    {
        using var span = ActivitySource.StartActivity("discord.rich_presence.update", ActivityKind.Client);
        span?.SetTag("discord.activity.name", activity.Name);
        span?.SetTag("discord.activity.details", activity.Details);
        span?.SetTag("discord.activity.state", activity.State);
        ActivityContext context = span?.Context ?? new ActivityContext();
        activity.Party.Id = "OT;" + context.TraceId.ToHexString() + ";" + context.SpanId.ToHexString() + ";" + context.TraceState;
        span?.AddTag("opentelemetry.context.size", activity.Party.Id.Length);
        lock (discord)
        {
            Console.WriteLine(DateTime.Now + ": " + activity.Name + ", " + activity.Details + ", " + activity.State);
            discord.GetActivityManager().UpdateActivity(activity, null /*result => { }*/);
        }
    }

    private void Run()
    {
        while (active)
        {
            Thread.Sleep(1000);
            lock(discord) {
                discord.RunCallbacks();
            }
        }
    }

    public void Dispose()
    {
        active = false;
        thread.Join();
        discord.Dispose();
    }
}

