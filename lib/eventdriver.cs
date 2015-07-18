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

    // This is actually supposed to be an enum
    public const int Milliseconds = 1;
    public const int Frames = 83; // Eh
    public const int Seconds = 1000;
    public const int Minutes = 60000;

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

    public void Schedule(double delay, int units, Action<MyGridProgram, EventDriver> action = null)
    {
        delay = Math.Max(delay, 0.0);
        var when = TimeSinceStart;
        switch (units)
        {
            case Milliseconds:
                when += TimeSpan.FromMilliseconds(delay);
                break;
            case Frames:
                when += TimeSpan.FromMilliseconds(1000.0 * delay / 60.0);
                break;
            case Seconds:
                when += TimeSpan.FromSeconds(delay);
                break;
            case Minutes:
                when += TimeSpan.FromMinutes(delay);
                break;
            default:
                throw new Exception("EventDriver.Schedule: Unknown units");
        }

        var future = new FutureAction(when, action);
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

    public void Schedule(uint frames, Action<MyGridProgram, EventDriver> action = null)
    {
        Schedule(frames, Frames, action);
    }
}
