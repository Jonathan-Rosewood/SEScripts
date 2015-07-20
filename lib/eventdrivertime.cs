public class EventDriver
{
    public struct FutureAction : IComparable<FutureAction>
    {
        public TimeSpan When;
        public Action<MyGridProgram, EventDriver> Action;

        public FutureAction(TimeSpan when, Action<MyGridProgram, EventDriver> action = null)
        {
            When = when;
            Action = action;
        }

        public int CompareTo(FutureAction other)
        {
            return When.CompareTo(other.When);
        }
    }

    private const double SecondsPerTick = 1.0 / 60.0;
    private const double TimeBuffer = SecondsPerTick / 2.0; // Adjust all times by this just because

    // Why is there no standard priority queue implementation?
    private readonly LinkedList<FutureAction> Queue = new LinkedList<FutureAction>();
    public TimeSpan TimeSinceStart
    {
        get { return m_timeSinceStart; }
        private set { m_timeSinceStart = value; }
    }
    private TimeSpan m_timeSinceStart = TimeSpan.FromSeconds(0);

    // Returns true if main should run
    public bool Tick(MyGridProgram program)
    {
        TimeSinceStart += program.ElapsedTime;

        bool result = false;
        while (Queue.First != null &&
               Queue.First.Value.When <= TimeSinceStart)
        {
            var action = Queue.First.Value.Action;
            Queue.RemoveFirst();
            if (action != null)
            {
                action(program, this);
            }
            else
            {
                result = true;
            }
        }
        return result;
    }

    public void Schedule(double seconds, Action<MyGridProgram, EventDriver> action = null)
    {
        seconds = Math.Max(seconds - TimeBuffer, 0.0); // Adjust by 1/2 tick time
        var future = new FutureAction(TimeSinceStart + TimeSpan.FromSeconds(seconds), action);
        for (var current = Queue.First;
             current != null;
             current = current.Next)
        {
            if (future.CompareTo(current.Value) < 0)
            {
                // Insert before this one
                Queue.AddBefore(current, future);
                return;
            }
        }
        // Just add at the end
        Queue.AddLast(future);
    }

    public void Schedule(ulong delay, Action<MyGridProgram, EventDriver> action = null)
    {
        // Best estimate
        Schedule(delay * SecondsPerTick, action);
    }
}
