//@ commons
public class EventDriver
{
    public struct FutureTickAction : IComparable<FutureTickAction>
    {
        public ulong When;
        public Action<ZACommons, EventDriver> Action;

        public FutureTickAction(ulong when, Action<ZACommons, EventDriver> action = null)
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
        public Action<ZACommons, EventDriver> Action;

        public FutureTimeAction(TimeSpan when, Action<ZACommons, EventDriver> action = null)
        {
            When = when;
            Action = action;
        }

        public int CompareTo(FutureTimeAction other)
        {
            return When.CompareTo(other.When);
        }
    }

    private const float TicksPerSecond = 60.0f;

    // Why is there no standard priority queue implementation?
    private readonly LinkedList<FutureTickAction> TickQueue = new LinkedList<FutureTickAction>();
    private readonly LinkedList<FutureTimeAction> TimeQueue = new LinkedList<FutureTimeAction>();
    private ulong Ticks; // Not a reliable measure of time because of variable update frequency.

    public TimeSpan TimeSinceStart { get; private set; }

    public EventDriver()
    {
        TimeSinceStart = TimeSpan.FromSeconds(0);
    }

    private void KickTimer(ZACommons commons)
    {
        // Rules are simple. If we have something in the tick queue, Update1.
        // Otherwise, set update frequency appropriately (1, 10, 100, or none).
        if (TickQueue.First != null)
        {
            commons.Program.Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        else if (TimeQueue.First != null)
        {
            var next = (float)(TimeQueue.First.Value.When.TotalSeconds - TimeSinceStart.TotalSeconds);
            if (next < (10.0f / TicksPerSecond))
            {
                commons.Program.Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }
            else if (next < (100.0f / TicksPerSecond))
            {
                commons.Program.Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }
            else
            {
                commons.Program.Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
        }
        else
        {
            // If both queues are empty, we stop running
            commons.Program.Runtime.UpdateFrequency = UpdateFrequency.None;
        }
    }

    public void Tick(ZACommons commons, Action mainAction = null,
                     Action preAction = null,
                     Action postAction = null)
    {
        Ticks++;
        TimeSinceStart += commons.Program.Runtime.TimeSinceLastRun;

        bool runMain = false;

        if (preAction != null) preAction();

        // Process each queue independently
        while (TickQueue.First != null &&
               TickQueue.First.Value.When <= Ticks)
        {
            var action = TickQueue.First.Value.Action;
            TickQueue.RemoveFirst();
            if (action != null)
            {
                action(commons, this);
            }
            else
            {
                runMain = true;
            }
        }

        while (TimeQueue.First != null &&
               TimeQueue.First.Value.When <= TimeSinceStart)
        {
            var action = TimeQueue.First.Value.Action;
            TimeQueue.RemoveFirst();
            if (action != null)
            {
                action(commons, this);
            }
            else
            {
                runMain = true;
            }
        }

        if (runMain && mainAction != null) mainAction();

        if (postAction != null) postAction();

        KickTimer(commons);
    }

    public void Schedule(ulong delay, Action<ZACommons, EventDriver> action = null)
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

    public void Schedule(double seconds, Action<ZACommons, EventDriver> action = null)
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
