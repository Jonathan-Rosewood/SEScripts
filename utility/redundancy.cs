//@ commons eventdriver dockinghandler
public class RedundancyManager : DockingHandler
{
    private const double RunDelay = 3.0;
    private const char COUNT_DELIMITER = ':';

    private bool IsDocked = true;

    public void Docked(ZACommons commons, EventDriver eventDriver)
    {
        IsDocked = true;
    }

    public void Undocked(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked)
        {
            IsDocked = false;
            eventDriver.Schedule(RunDelay, DHRun);
        }
    }

    public void DHRun(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return;

        Run(commons);

        eventDriver.Schedule(RunDelay, DHRun);
    }

    public void Run(ZACommons commons)
    {
        for (var e = commons.GetBlockGroupsWithPrefix(REDUNDANCY_PREFIX).GetEnumerator(); e.MoveNext();)
        {
            var group = e.Current;

            // Figure out how many to maintain
            var parts = group.Name.Split(new char[] { COUNT_DELIMITER }, 2);
            var count = 1;
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[1], out count))
                {
                    count = Math.Max(count, 1);
                }
                else
                {
                    count = 1;
                }
            }

            var running = 0;
            var spares = new LinkedList<IMyTerminalBlock>();

            for (var f = group.Blocks.GetEnumerator(); f.MoveNext();)
            {
                var block = f.Current as IMyFunctionalBlock;
                if (block != null && block.CubeGrid == commons.Me.CubeGrid &&
                    block.IsFunctional)
                {
                    if (block.IsWorking && block.Enabled)
                    {
                        running++;
                    }
                    else
                    {
                        spares.AddLast(block);
                    }
                }
            }

            while (running < count && spares.First != null)
            {
                var block = spares.First.Value;
                spares.RemoveFirst();
                block.GetActionWithName("OnOff_On").Apply(block);
                running++;
            }
        }
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        Run(commons);
        eventDriver.Schedule(RunDelay, Run);
    }
}
