using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class RichPresence : IRichPresence
{
    private static readonly ActivitySource ActivitySource = new ActivitySource(IRichPresence.ACTIVITY_SOURCE_NAME);

    private ILogger logger;
    private Discord.Discord discord;
    private Thread thread;
    private bool active;

    public RichPresence(ILoggerFactory loggerFactory, long clientID /* https://discord.com/developers/applications */, uint steamID)
    {
        logger = loggerFactory.CreateLogger<RichPresence>();
        discord = new Discord.Discord(clientID, (ulong) Discord.CreateFlags.NoRequireDiscord);
        thread = new Thread(() => Run());
        active = true;

        // discord.SetLogHook(Discord.LogLevel.Debug, null /*(level, message) => Console.WriteLine(message)*/);
        if (steamID != 0) discord.GetActivityManager().RegisterSteam(steamID);
        thread.Start();
    }

    public virtual void Clear()
    {
        using var span = ActivitySource.StartActivity("discord.rich_presence.clear");
        lock (discord)
        {
            logger.Log(LogLevel.Information, "Clear");
            discord.GetActivityManager().ClearActivity(result => { });
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
            logger.Log(LogLevel.Information, "Update {0}, {1}, {2}", activity.Name, activity.Details, activity.State);
            discord.GetActivityManager().UpdateActivity(activity, result => { });
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

