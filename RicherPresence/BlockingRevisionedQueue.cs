using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BlockingRevisionedQueue<T>
{

    public delegate long RevisionGetter(T item);

    private readonly object monitor = new object();
    private readonly int maxSize;
    private readonly RevisionGetter getRevision;
    private readonly long revisionTimeout;
    private readonly SortedList<long, T> queue = new SortedList<long, T>();

    private long nextRevision;
    private bool dequeuer;

    public BlockingRevisionedQueue(int maxSize, RevisionGetter getRevision, long revisionTimeout, long firstRevision)
    {
        this.maxSize = maxSize;
        this.getRevision = getRevision;
        this.revisionTimeout = revisionTimeout;
        this.nextRevision = firstRevision;
        this.dequeuer = false;
    }

    public void Reset(long firstRevision)
    {
        lock (monitor) {
            Clear();
            nextRevision = firstRevision;
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
        }
    }

    public bool Enqueue(T item, bool blockWhenFull)
    {
        lock (monitor)
        {
            while (queue.Count == maxSize)
                if (blockWhenFull) Monitor.Wait(monitor);
                else return false;
            long revision = getRevision.Invoke(item);
            if (revision < nextRevision) return false;
            queue.Add(revision, item);
            Monitor.PulseAll(monitor);
            return true;
        }
    }

    public T Dequeue(bool skipWhenOutOfOrder)
    {
        lock (monitor)
        {
            while (dequeuer) Monitor.Wait(monitor);
            try
            {
                dequeuer = true;
                while (queue.Count == 0) Monitor.Wait(monitor);
                long end = skipWhenOutOfOrder ? Environment.TickCount64 + revisionTimeout : long.MaxValue;
                while (!queue.ContainsKey(nextRevision) && Environment.TickCount64 < end) Monitor.Wait(monitor, Math.Max(1, (int)(end - Environment.TickCount64)));
                while (!queue.ContainsKey(nextRevision)) nextRevision++;
                T item = queue[nextRevision];
                queue.Remove(nextRevision);
                nextRevision++;
                Monitor.PulseAll(monitor);
                return item;
            }
            finally
            {
                dequeuer = false;
            }
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
