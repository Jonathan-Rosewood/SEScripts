public class EventDriver
{
    public struct FutureTickAction : IComparable<FutureTickAction>
    {
        public ulong When;
        public Action<MyGridProgram, EventDriver> Action;

        public FutureTickAction(ulong when, Action<MyGridProgram, EventDriver> action = null)
        {
            When = when;
            Action = action;
        }

        public int CompareTo(FutureTickAction other)
        {
            return When.CompareTo(other.When);
        }
    }

    public struct FutureTimeAction : IComparable<FutureTimeAction>
    {
        public TimeSpan When;
        public Action<MyGridProgram, EventDriver> Action;

        public FutureTimeAction(TimeSpan when, Action<MyGridProgram, EventDriver> action = null)
        {
            When = when;
            Action = action;
        }

        public int CompareTo(FutureTimeAction other)
        {
            return When.CompareTo(other.When);
        }
    }

    private const ulong TicksPerSecond = 60;

    // Why is there no standard priority queue implementation?
    private readonly LinkedList<FutureTickAction> TickQueue = new LinkedList<FutureTickAction>();
    private readonly LinkedList<FutureTimeAction> TimeQueue = new LinkedList<FutureTimeAction>();

    private ulong Ticks; // Not a reliable measure of time because of variable timer delay.

    public TimeSpan TimeSinceStart
    {
        get { return m_timeSinceStart; }
        private set { m_timeSinceStart = value; }
    }
    private TimeSpan m_timeSinceStart = TimeSpan.FromSeconds(0);

    private string TimerName, TimerGroup;

    // If neither timerName nor timerGroup are given, it's assumed the timer will kick itself
    public EventDriver(string timerName = null, string timerGroup = null)
    {
        TimerName = timerName;
        TimerGroup = timerGroup;
    }

    private void KickTimer(MyGridProgram program)
    {
        IMyTimerBlock timer = null;
        // Name takes priority over group
        if (TimerName != null)
        {
            var blocks = new List<IMyTerminalBlock>();
            program.GridTerminalSystem.SearchBlocksOfName(TimerName, blocks,
                                                          block => block is IMyTimerBlock &&
                                                          block.CubeGrid == program.Me.CubeGrid);
            if (blocks.Count > 0)
            {
                timer = blocks[0] as IMyTimerBlock;
            }
        }
        else if (TimerGroup != null)
        {
            var group = ZALibrary.GetBlockGroupWithName(program, TimerGroup);
            if (group != null && group.Blocks.Count > 0)
            {
                // NB We only check the first block of the group, whatever that may be
                timer = group.Blocks[0] as IMyTimerBlock;
            }
        }

        if (timer != null)
        {
            // Rules are simple. If we have something in the tick queue, trigger now.
            // Otherwise, set timer delay appropriately (minimum 1 second) and kick.
            // If you want sub-second accuracy, always use ticks.
            if (TickQueue.First != null)
            {
                timer.GetActionWithName("TriggerNow").Apply(timer);
            }
            else if (TimeQueue.First != null)
            {
                var next = (float)(TimeQueue.First.Value.When.TotalSeconds - TimeSinceStart.TotalSeconds);
                // Constrain appropriately (not sure if this will be done for us or if it
                // will just throw). Just do it to be safe.
                next = Math.Max(next, timer.GetMininum<float>("TriggerDelay"));
                next = Math.Min(next, timer.GetMaximum<float>("TriggerDelay"));

                timer.SetValue<float>("TriggerDelay", next);
                timer.GetActionWithName("Start").Apply(timer);
            }
            // NB If both queues are empty, we stop running
        }
    }

    // Returns true if main should run
    public bool Tick(MyGridProgram program)
    {
        Ticks++;
        TimeSinceStart += program.ElapsedTime;

        bool result = false;

        // Process each queue independently
        while (TickQueue.First != null &&
               TickQueue.First.Value.When <= Ticks)
        {
            var action = TickQueue.First.Value.Action;
            TickQueue.RemoveFirst();
            if (action != null)
            {
                action(program, this);
            }
            else
            {
                result = true;
            }
        }

        while (TimeQueue.First != null &&
               TimeQueue.First.Value.When <= TimeSinceStart)
        {
            var action = TimeQueue.First.Value.Action;
            TimeQueue.RemoveFirst();
            if (action != null)
            {
                action(program, this);
            }
            else
            {
                result = true;
            }
        }

        KickTimer(program);

        return result;
    }

    public void Schedule(ulong delay, Action<MyGridProgram, EventDriver> action = null)
    {
        var future = new FutureTickAction(Ticks + delay, action);
        for (var current = TickQueue.First;
             current != null;
             current = current.Next)
        {
            if (future.CompareTo(current.Value) < 0)
            {
                // Insert before this one
                TickQueue.AddBefore(current, future);
                return;
            }
        }
        // Just add at the end
        TickQueue.AddLast(future);
    }

    public void Schedule(double seconds, Action<MyGridProgram, EventDriver> action = null)
    {
        var delay = Math.Max(seconds, 0.0);

        var future = new FutureTimeAction(TimeSinceStart + TimeSpan.FromSeconds(delay), action);
        for (var current = TimeQueue.First;
             current != null;
             current = current.Next)
        {
            if (future.CompareTo(current.Value) < 0)
            {
                // Insert before this one
                TimeQueue.AddBefore(current, future);
                return;
            }
        }
        // Just add at the end
        TimeQueue.AddLast(future);
    }
}
