public class EventDriver
{
    public struct FutureAction : IComparable<FutureAction>
    {
        public ulong When;
        public Action<MyGridProgram, EventDriver> Action;

        public FutureAction(ulong when, Action<MyGridProgram, EventDriver> action = null)
        {
            When = when;
            Action = action;
        }

        public int CompareTo(FutureAction other)
        {
            return When.CompareTo(other.When);
        }
    }

    private const ulong TicksPerSecond = 60;

    // Why is there no standard priority queue implementation?
    private readonly LinkedList<FutureAction> Queue = new LinkedList<FutureAction>();
    public ulong Ticks
    {
        get { return m_ticks; }
        private set { m_ticks = value; }
    }
    private ulong m_ticks = 0;
    public TimeSpan TimeSinceStart
    {
        get { return m_timeSinceStart; }
        private set { m_timeSinceStart = value; }
    }
    private TimeSpan m_timeSinceStart = TimeSpan.FromSeconds(0);

    // Returns true if main should run
    public bool Tick(MyGridProgram program)
    {
        Ticks++;
        TimeSinceStart += program.ElapsedTime;

        bool result = false;
        while (Queue.First != null &&
               Queue.First.Value.When <= Ticks)
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

    public void Schedule(ulong delay, Action<MyGridProgram, EventDriver> action = null)
    {
        var future = new FutureAction(Ticks + delay, action);
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

    public void Schedule(double seconds, Action<MyGridProgram, EventDriver> action = null)
    {
        seconds = Math.Max(seconds, 0.0);
        // Best guess
        ulong delay = (ulong)(seconds * TicksPerSecond + 0.5);
        Schedule(delay, action);
    }
}
