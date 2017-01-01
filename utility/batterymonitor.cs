//@ commons eventdriver dockinghandler
public class BatteryMonitor : DockingHandler
{
    public interface LowBatteryHandler
    {
        void LowBattery(ZACommons commons, EventDriver eventDriver,
                        bool started);
    }

    private const double RunDelay = 5.0;

    private readonly LowBatteryHandler lowBatteryHandler;

    private bool IsDocked = true;
    private bool Triggered = false;

    public BatteryMonitor(LowBatteryHandler lowBatteryHandler = null)
    {
        this.lowBatteryHandler = lowBatteryHandler;
    }

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
            Triggered = false;

            IsDocked = false;
            eventDriver.Schedule(RunDelay, Run);
        }
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        IsDocked = false;
        eventDriver.Schedule(0.0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        if (IsDocked) return;

        RunInternal(commons, eventDriver);

        eventDriver.Schedule(RunDelay, Run);
    }

    private void RunInternal(ZACommons commons, EventDriver eventDriver)
    {
        var lowBattery = ZACommons.GetBlockWithName<IMyTimerBlock>(commons.Blocks, LOW_BATTERY_NAME);
        // Don't bother if there's no timer block or handler
        if (lowBatteryHandler == null && lowBattery == null) return;

        var batteries = ZACommons.GetBlocksOfType<IMyBatteryBlock>(commons.Blocks, battery => battery.IsFunctional && battery.Enabled);

        // Avoid divide-by-zero in case there are no batteries
        if (batteries.Count == 0) return;

        var currentStoredPower = 0.0f;
        var maxStoredPower = 0.0f;

        // Hmm, doesn't check battery recharge state...
        // With the "full-auto mode" (if it worked as advertised),
        // it probably doesn't make sense to check input/output state anyway
        foreach (var block in batteries)
        {
            var battery = block as IMyBatteryBlock;

            currentStoredPower += battery.CurrentStoredPower;
            maxStoredPower += battery.MaxStoredPower;
        }

        var batteryPercent = currentStoredPower / maxStoredPower;

        if (!Triggered && batteryPercent < BATTERY_THRESHOLD)
        {
            Triggered = true;
            if (lowBatteryHandler != null) lowBatteryHandler.LowBattery(commons, eventDriver, true);
            if (lowBattery != null) lowBattery.ApplyAction("Start");
        }
        else if (Triggered && batteryPercent >= BATTERY_THRESHOLD)
        {
            Triggered = false;
            if (lowBatteryHandler != null) lowBatteryHandler.LowBattery(commons, eventDriver, false);
        }
    }
}
