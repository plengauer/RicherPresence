using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class ConstantOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly T options;

    public ConstantOptionsMonitor(T options)
    {
        this.options = options;
    }

    public T CurrentValue => options;

    public T Get(string name)
    {
        return options;
    }

    private class Dummy : IDisposable
    {
        public void Dispose()
        {
            // nothing to do
        }
    }

    public IDisposable OnChange(Action<T, string> listener)
    {
        // no changes, so no reason to remember listeners
        return new Dummy();
    }
}

