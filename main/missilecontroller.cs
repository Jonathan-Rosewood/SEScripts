private readonly MissileGuidance missileGuidance = new MissileGuidance();
private readonly RandomDecoy randomDecoy = new RandomDecoy();

void Main(string argument)
{
    missileGuidance.Run(this);
    randomDecoy.Run(this);

    // Kick timer
    var timers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);
    var timer = timers[0];
    timer.GetActionWithName("TriggerNow").Apply(timer);
}
