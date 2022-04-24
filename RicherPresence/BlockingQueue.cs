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

    public BlockingQueue(int maxSize)
    {
        this.maxSize = maxSize;
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
        }
    }

    public bool Enqueue(T item, bool blockWhenFull)
    {
        lock (monitor)
        {
            while (queue.Count == maxSize)
                if (blockWhenFull) Monitor.Wait(monitor);
                else return false;
            queue.AddLast(item);
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
            Monitor.PulseAll(monitor);
            return item;
        }
    }

}
