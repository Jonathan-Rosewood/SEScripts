private readonly EventDriver eventDriver = new EventDriver();
private readonly MissileGuidance missileGuidance = new MissileGuidance();
private readonly RandomDecoy randomDecoy = new RandomDecoy();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        eventDriver.Schedule(0, EventDriver.Milliseconds, missileGuidance.Run);
        eventDriver.Schedule(0, EventDriver.Milliseconds, randomDecoy.Run);
    }

    eventDriver.Tick(this);

    // Kick timer
    var timers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);
    var timer = timers[0];
    timer.GetActionWithName("TriggerNow").Apply(timer);
}
