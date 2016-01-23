public class BatteryMonitor : DockingHandler
{
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
            eventDriver.Schedule(1.0, Run);
        }
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return;

        Run(commons);

        eventDriver.Schedule(1.0, Run);
    }

    public void Run(ZACommons commons)
    {
        var lowBattery = ZACommons.GetBlockWithName<IMyTimerBlock>(commons.Blocks, LOW_BATTERY_NAME);
        // Don't bother if there's no timer block
        if (lowBattery == null) return;

        var batteries = ZACommons.GetBlocksOfType<IMyBatteryBlock>(commons.Blocks, battery => battery.IsFunctional && ((IMyBatteryBlock)battery).Enabled);

        // Avoid divide-by-zero in case there are no batteries
        if (batteries.Count == 0) return;

        var currentStoredPower = 0.0f;
        var maxStoredPower = 0.0f;

        // Hmm, doesn't check battery recharge state...
        for (var e = batteries.GetEnumerator(); e.MoveNext();)
        {
            var battery = e.Current as IMyBatteryBlock;

            currentStoredPower += battery.CurrentStoredPower;
            maxStoredPower += battery.MaxStoredPower;
        }

        var batteryPercent = currentStoredPower / maxStoredPower;

        if (lowBattery.Enabled && !lowBattery.IsCountingDown && batteryPercent < BATTERY_THRESHOLD)
        {
            lowBattery.GetActionWithName("Start").Apply(lowBattery);
        }
        else if (!lowBattery.Enabled && batteryPercent >= BATTERY_THRESHOLD)
        {
            lowBattery.GetActionWithName("OnOff_On").Apply(lowBattery);
        }
    }
}
