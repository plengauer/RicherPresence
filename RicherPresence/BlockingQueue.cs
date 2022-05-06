using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BlockingQueue<T>
{

    private readonly int maxSize;
    private readonly object monitor = new object();
    private readonly LinkedList<T> queue = new LinkedList<T>();
    private int revision;

    public BlockingQueue(int maxSize)
    {
        this.maxSize = maxSize;
        revision = 0;
    }

    public int Revision
    {
        get
        {
            lock (monitor)
            {
                return revision;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (monitor)
            {
                return queue.Count;
            }
        }
    }

    public void Clear()
    {
        lock (monitor)
        {
            queue.Clear();
            Monitor.PulseAll(monitor);
            revision++;
        }
    }

    public bool Enqueue(T item)
    {
        lock (monitor)
        {
            while (maxSize > 0 && queue.Count == maxSize) Monitor.Wait(monitor);
            queue.AddLast(item);
            revision++;
            Monitor.PulseAll(monitor);
            return true;
        }
    }

    public T Dequeue()
    {
        lock (monitor)
        {
            while (queue.Count == 0) Monitor.Wait(monitor);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            T item = queue.First.Value;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            queue.RemoveFirst();
            revision++;
            Monitor.PulseAll(monitor);
            return item;
        }
    }

    public void WaitForEmpty()
    {
        lock (monitor)
        {
            while (Count > 0) Monitor.Wait(monitor);
        }
    }

}
