using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public static class ProcessEx
{

    public static void WaitForExitFixed(this Process thiz)
    {
        // thiz.WaitForExit();
        // this doesnt react well to interrupt calls
        // apparently, if interrupt is called, subsequent Monitor.Wait or Thread.Sleep sometimes do not throw a ThreadInterruptedException
        while (!thiz.HasExited) Thread.Sleep(10);
    }

}

