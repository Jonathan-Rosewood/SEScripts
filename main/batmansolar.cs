public class TimerBlockPowerDrainHandler : BatteryManager.PowerDrainHandler
{
    public void PowerDrainStarted(ZACommons commons)
    {
        ZACommons.StartTimerBlockWithName(commons.Blocks, POWER_DRAIN_START_NAME);
    }

    public void PowerDrainEnded(ZACommons commons)
    {
        ZACommons.StartTimerBlockWithName(commons.Blocks, POWER_DRAIN_END_NAME);
    }
}

public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly BatteryManager batteryManager = new BatteryManager(new TimerBlockPowerDrainHandler());

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    batteryManager.HandleCommand(commons, argument);

    eventDriver.Tick(commons, () =>
            {
                batteryManager.Run(commons);

                eventDriver.Schedule(1.0);
            });
}
