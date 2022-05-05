using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScreenSimulator : Screen
{
    private string[] files;
    private int index;

    public ScreenSimulator(string directory)
    {
        this.files = Directory.GetFiles(directory).AsEnumerable().Where(file => file.EndsWith(".bmp") || file.EndsWith(".png")).ToArray();
        this.index = 0;
        Array.Sort(files);
    }

    public string? Capture(long id)
    {
        lock (this)
        {
            Monitor.PulseAll(this);
            return index < files.Length ? files[index++] : null;
        }
    }

    public bool IsDone()
    {
        return index >= files.Length;
    }

    public void Join()
    {
        lock(this)
        {
            while(!IsDone()) Monitor.Wait(this);
        }
    }
}
