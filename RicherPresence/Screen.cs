using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface Screen
{
    string Capture(long myID);

    bool IsDone();
}
