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

private readonly BatteryManager batteryManager = new BatteryManager(new TimerBlockPowerDrainHandler());

void Main(string argument)
{
    ZALibrary.Ship ship = new ZALibrary.Ship(this);

    batteryManager.HandleCommand(this, ship, argument);
    batteryManager.Run(this, ship);

    ZALibrary.KickLoopTimerBlock(this, argument);
}
