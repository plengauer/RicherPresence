using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BlockingRevisionedQueue<T>
{

    private struct Element
    {
        public long Time { get; set; }
        public T Value { get; set; }
    }

    public delegate long RevisionGetter(T item);

    private readonly object monitor = new object();
    private readonly int maxSize;
    private readonly RevisionGetter getRevision;
    private readonly long revisionTimeout;
    private readonly SortedList<long, Element> queue = new SortedList<long, Element>();
    private int revision;

    private long nextRevision;
    private bool dequeuer;

    public BlockingRevisionedQueue(int maxSize, RevisionGetter getRevision, long revisionTimeout, long firstRevision)
    {
        this.maxSize = maxSize;
        this.getRevision = getRevision;
        this.revisionTimeout = revisionTimeout;
        this.revision = 0;
        this.nextRevision = firstRevision;
        this.dequeuer = false;
    }

    public void Reset(long firstRevision)
    {
        lock (monitor) {
            Clear();
            nextRevision = firstRevision;
            revision++;
        }
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
            revision++;
            Monitor.PulseAll(monitor);
        }
    }

    public bool Enqueue(T item)
    {
        lock (monitor)
        {
            while (maxSize > 0 && queue.Count == maxSize) Monitor.Wait(monitor);
            long revision = getRevision.Invoke(item);
            if (revision < nextRevision) return false;
            queue.Add(revision, new Element() { Time = Environment.TickCount64, Value = item });
            revision++;
            Monitor.PulseAll(monitor);
            return true;
        }
    }

    public T Dequeue()
    {
        lock (monitor)
        {
            while (dequeuer) Monitor.Wait(monitor);
            try
            {
                dequeuer = true;
                while (queue.Count == 0) Monitor.Wait(monitor);
                long end = queue.First().Value.Time + revisionTimeout;
                while (!queue.ContainsKey(nextRevision) && Environment.TickCount64 < end) Monitor.Wait(monitor, Math.Max(1, (int)(end - Environment.TickCount64)));
                while (!queue.ContainsKey(nextRevision)) nextRevision++;
                Element element = queue[nextRevision];
                queue.Remove(nextRevision);
                nextRevision++;
                revision++;
                Monitor.PulseAll(monitor);
                return element.Value;
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
