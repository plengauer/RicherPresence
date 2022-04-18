using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IRichPresence : IDisposable
{
    public const string ACTIVITY_SOURCE_NAME = "RichPresenceActivitySource";
    public const string METER_SOURCE_NAME = "RichPresenceMeterSource";
    public abstract void Update(Discord.Activity activity);
    public abstract void Clear();
}
