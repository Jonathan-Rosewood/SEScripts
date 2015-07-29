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
    private ulong Ticks = 0; // Would be nice to expose this, but would not be portable w/ other implementation
    public TimeSpan TimeSinceStart
    {
        get { return m_timeSinceStart; }
        private set { m_timeSinceStart = value; }
    }
    private TimeSpan m_timeSinceStart = TimeSpan.FromSeconds(0);

    private string TimerName = null, TimerGroup = null;
    public bool FrameTicks { get; set; }

    public EventDriver(string timerName = null, string timerGroup = null, bool frameTicks = true)
    {
        TimerName = timerName;
        TimerGroup = timerGroup;
        FrameTicks = (timerName != null || timerGroup != null) ? frameTicks : true;
    }

    private void kickTimer(MyGridProgram program)
    {
        IMyTimerBlock timer = null;
        if (TimerName != null)
        {
            var block = program.GridTerminalSystem.GetBlockWithName(TimerName);
            timer = block as IMyTimerBlock;
        }
        else if (TimerGroup != null)
        {
            var group = ZALibrary.GetBlockGroupWithName(program, TimerGroup);
            if (group != null && group.Blocks.Count > 0)
            {
                timer = group.Blocks[0] as IMyTimerBlock;
            }
        }
        if (timer != null)
        {
            if (FrameTicks)
            {
                timer.GetActionWithName("TriggerNow").Apply(timer);
            }
            else
            {
                timer.SetValue<float>("TriggerDelay", 1.0f);
                timer.GetActionWithName("Start").Apply(timer);
                Ticks += TicksPerSecond; // groan
            }
        }
    }

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

        if (TimerName != null || TimerGroup != null) kickTimer(program);

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
