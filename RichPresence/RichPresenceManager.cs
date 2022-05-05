using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public abstract class RichPresenceManager : IDisposable
{

    private string processName;

    private Thread thread;
    private bool running;

    private object monitor;
    private bool active;

    public RichPresenceManager(string processName)
    {
        this.processName = processName;

        thread = new Thread(() => Run());
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
            using (IRichPresence presence = CreateRichPresence())
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

    protected abstract IRichPresence CreateRichPresence();

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
            active = true;
            Monitor.PulseAll(monitor);
        }
    }

    protected virtual void Stop(IRichPresence presence) {
        lock (monitor)
        {
            active = false;
            Monitor.PulseAll(monitor);
        }
    }

    public void Dispose(bool force)
    {
        if (!force)
        {
            lock (monitor)
            {
                while (active) Monitor.Wait(monitor);
            }
        }
        running = false;
        thread.Join();
    }

    public virtual void Dispose()
    {
        Dispose(true);
    }
}
