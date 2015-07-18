private readonly EventDriver eventDriver = new EventDriver();
private readonly MissileGuidance missileGuidance = new MissileGuidance();
private readonly RandomDecoy randomDecoy = new RandomDecoy();
private readonly MissileLaunch missileLaunch = new MissileLaunch();

private bool FirstRun = true;

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = false;
        missileLaunch.Init(this, eventDriver, missileGuidance);
        randomDecoy.Init(this, eventDriver);
        // Guidance will be initialized by launch
    }

    eventDriver.Tick(this);

    // Kick timer
    var timers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);
    var timer = timers[0];
    timer.GetActionWithName("TriggerNow").Apply(timer);
}
