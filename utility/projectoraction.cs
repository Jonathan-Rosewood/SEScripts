//@ commons eventdriver
public class ProjectorAction
{
    private const double RunDelay = 1.0;
    private const char ACTION_DELIMITER = ':';

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length < 2 || parts[0] != "projaction") return;

        DoAction(commons, eventDriver, parts[1]);
    }

    private void DoAction(ZACommons commons, EventDriver eventDriver,
                          string name)
    {
        string timers;
        var projector = GetProjector(commons, name, out timers);
        if (projector == null) return;

        var parts = timers.Split(new char[] { ACTION_DELIMITER }, 2);
        var startTimer = parts[0].Trim();
        var endTimer = parts.Length > 1 ? parts[1].Trim() : "";

        // No timers, nothing to do
        if ((startTimer.Length + endTimer.Length) == 0) return;

        // Enable projector, if it isn't already
        projector.SetValue<bool>("OnOff", true);

        // Start start timer, if we have one
        StartTimerBlock(commons, startTimer);

        // Start loop
        eventDriver.Schedule(RunDelay, new ProjectorActionHelper(name, endTimer).Run);
    }

    private static IMyProjector GetProjector(ZACommons commons, string name,
                                             out string rest)
    {
        var groupPrefix = PROJECTOR_ACTION_PREFIX + name + ACTION_DELIMITER;
        var groups = commons.GetBlockGroupsWithPrefix(groupPrefix);
        if (groups.Count > 0)
        {
            var blocks = ZACommons.GetBlocksOfType<IMyProjector>(groups[0].Blocks);
            for (var e = blocks.GetEnumerator(); e.MoveNext();)
            {
                var block = e.Current;
                // Just take first one
                if (block.IsFunctional)
                {
                    rest = groups[0].Name.Substring(groupPrefix.Length);
                    return (IMyProjector)blocks[0];
                }
            }
        }
        rest = default(string);
        return null;
    }

    private static void StartTimerBlock(ZACommons commons, string name)
    {
        if (name.Length == 0) return;
        var timers = ZACommons.SearchBlocksOfName(commons.Blocks, name);
        // Find the first timer block that's enabled
        for (var e = timers.GetEnumerator(); e.MoveNext();)
        {
            var timer = e.Current as IMyTimerBlock;
            if (timer != null && timer.Enabled)
            {
                // And start it if it isn't already counting down
                if (!timer.IsCountingDown) timer.ApplyAction("Start");
                return;
            }
        }
    }

    // Seems saner to do it this way than use a recursive closure
    public class ProjectorActionHelper
    {
        private readonly string Name;
        private readonly string EndTimer;

        public ProjectorActionHelper(string name, string endTimer)
        {
            Name = name;
            EndTimer = endTimer;
        }

        public void Run(ZACommons commons, EventDriver eventDriver)
        {
            string rest;
            var projector = GetProjector(commons, Name, out rest);
            if (projector == null || !projector.Enabled ||
                projector.RemainingBlocks <= 0)
            {
                // All done
                StartTimerBlock(commons, EndTimer);
            }
            else
            {
                // Continue loop
                eventDriver.Schedule(RunDelay, Run);
            }
        }
    }
}
