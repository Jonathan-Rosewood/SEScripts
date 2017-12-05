//@ commons eventdriver dockinghandler
public class RedundancyManager : DockingHandler
{
    private const double RunDelay = 3.0;
    private const char COUNT_DELIMITER = ':';

    private bool IsDocked = true;

    public void PreDock(ZACommons commons, EventDriver eventDriver) { }

    public void DockingAction(ZACommons commons, EventDriver eventDriver,
                              bool docked)
    {
        if (docked)
        {
            IsDocked = true;
        }
        else if (IsDocked)
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
        foreach (var group in commons.GetBlockGroupsWithPrefix(REDUNDANCY_PREFIX))
        {
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
            var spares = new LinkedList<IMyFunctionalBlock>();

            foreach (var block in group.Blocks)
            {
                var fblock = block as IMyFunctionalBlock;
                if (fblock != null && fblock.CubeGrid == commons.Me.CubeGrid &&
                    fblock.IsFunctional)
                {
                    if (fblock.IsWorking && fblock.Enabled)
                    {
                        running++;
                    }
                    else
                    {
                        spares.AddLast(fblock);
                    }
                }
            }

            while (running < count && spares.First != null)
            {
                var block = spares.First.Value;
                spares.RemoveFirst();
                block.Enabled = true;
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
