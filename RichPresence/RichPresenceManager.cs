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
    private bool active;

    public RichPresenceManager(string processName)
    {
        this.processName = processName;

        thread = new Thread(() => Run());
        active = true;

        thread.Start();
    }

    private void Run()
    {
        Thread.Sleep(1000 * 5); // this thread is started in the ctor, but makes virtual calls. waiting is a hacky workaround in this case ...
        while (true)
        {
            while(active && !IsProcessRunning()) WaitForProcessChange();
            if (!active) return;
            using (IRichPresence presence = CreateRichPresence())
            {
                presence.Update(new Discord.Activity());
                Start(presence);
                while (active && IsProcessRunning()) WaitForProcessChange();
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

    protected virtual void Start(IRichPresence presence) { }

    protected virtual void Stop(IRichPresence presence) { }

    public virtual void Dispose()
    {
        active = false;
        thread.Join();
    }
}
