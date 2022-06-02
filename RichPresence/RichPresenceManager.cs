using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

public abstract class RichPresenceManager : IDisposable
{

    private string processName;
    private ILoggerFactory factory;

    private ILogger logger;
    private Thread thread;
    private bool running;

    private object monitor;
    private bool active;

    public RichPresenceManager(ILoggerFactory factory, string processName)
    {
        this.processName = processName;
        this.factory = factory;

        logger = factory.CreateLogger<RichPresenceManager>();
        thread = new Thread(() => Run()) { Name = "Rich Presence Manager" };
        running = true;

        monitor = new object();
        active = false;

        thread.Start();
    }

    public bool IsActive()
    {
        lock (monitor)
        {
            return active;
        }
    }

    private void Run()
    {
        Thread.Sleep(1000 * 5); // this thread is started in the ctor, but makes virtual calls. waiting is a hacky workaround in this case ...
        while (true)
        {
            while(running && !IsProcessRunning()) WaitForProcessChange();
            if (!running) return;
            using (IRichPresence presence = CreateRichPresence(factory))
            {
                presence.Update(new Discord.Activity());
                Start(presence);
                while (running && IsProcessRunning()) WaitForProcessChange();
                Stop(presence);
            }
        }
    }

    protected virtual void WaitForProcessChange()
    {
        Thread.Sleep(1000 * 10);
    }

    protected abstract IRichPresence CreateRichPresence(ILoggerFactory logger);

    protected virtual bool IsProcessRunning()
    {
        foreach (Process process in Process.GetProcesses())
        {
            if (process.ProcessName.Equals(processName))
            {
                return true;
            }
        }
        return false;
    }

    protected virtual void Start(IRichPresence presence) {
        lock (monitor)
        {
            logger.LogInformation("Starting");
            active = true;
            Monitor.PulseAll(monitor);
        }
    }

    protected virtual void Stop(IRichPresence presence) {
        lock (monitor)
        {
            active = false;
            Monitor.PulseAll(monitor);
            logger.LogInformation("Stopped");
        }
    }

    public void Dispose(bool force)
    {
        logger.LogInformation("Disposing ({0})", force ? "force" : "normal");
        if (!force)
        {
            lock (monitor)
            {
                while (active) Monitor.Wait(monitor);
            }
        }
        running = false;
        thread.Join();
        logger.LogInformation("Disposed ({0})", force ? "force" : "normal");
    }

    public virtual void Dispose()
    {
        logger.LogInformation("Disposing");
        Dispose(false);
        logger.LogInformation("Disposed");
    }
}
