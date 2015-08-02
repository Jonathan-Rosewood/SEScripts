public class TimerBlockPowerDrainHandler : BatteryManager.PowerDrainHandler
{
    public void PowerDrainStarted(ZALibrary.Ship ship)
    {
        ZALibrary.StartTimerBlockWithName(ship.Blocks, POWER_DRAIN_START_NAME);
    }

    public void PowerDrainEnded(ZALibrary.Ship ship)
    {
        ZALibrary.StartTimerBlockWithName(ship.Blocks, POWER_DRAIN_END_NAME);
    }
}

private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly BatteryManager batteryManager = new BatteryManager(new TimerBlockPowerDrainHandler());

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0.0);
    }

    ZALibrary.Ship ship = new ZALibrary.Ship(this);
    batteryManager.HandleCommand(this, ship, argument);

    if (eventDriver.Tick(this))
    {
        batteryManager.Run(this, ship);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}
