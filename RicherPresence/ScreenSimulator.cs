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
        this.files = Directory.GetFiles(directory);
        this.index = 0;
    }

    public string Capture(long id)
    {
        lock (this)
        {
            Monitor.PulseAll(this);
            while (index < files.Length)
            {
                string file = files[index++];
                if (file.EndsWith(".png") || file.EndsWith(".bmp")) return file;
            }
            return null;
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
