using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public static class ThreadEx
{

    public static void InterruptFixed(this Thread thiz)
    {
        // thiz.Interrupt();
        // sometimes interrupt seems not to wake the thread and it keeps blocking forever
        do
        {
            thiz.Interrupt();
        } while (!thiz.Join(10));
    }

}

