private readonly EventDriver eventDriver = new EventDriver(timerName: ZALIBRARY_LOOP_TIMER_BLOCK_NAME);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode();
private readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
private readonly SolarRotorController rotorController = new SolarRotorController();

private bool FirstRun = true;
private Rangefinder.LineSample first, second;
private StringBuilder rangefinderResult = new StringBuilder();

void Main(string argument)
{
    if (FirstRun)
    {
        FirstRun = true;
        eventDriver.Schedule(0.0);
    }

    var ship = new ZALibrary.Ship(this);
    dockingManager.HandleCommand(this, ship, argument);
    HandleCommand(this, argument);

    if (eventDriver.Tick(this))
    {
        // This really seems like it should be determined once per run
        var isConnected = ship.IsConnectedAnywhere();

        dockingManager.Run(this, ship, isConnected);
        safeMode.Run(this, ship, isConnected);
        batteryMonitor.Run(this, ship, isConnected);
        if (MAX_POWER_ENABLED) rotorController.Run(this);

        eventDriver.Schedule(1.0);
        eventDriver.KickTimer(this);
    }
}

private void HandleCommand(MyGridProgram program, string argument)
{
    var reference = GetReference(program);

    argument = argument.Trim().ToLower();
    switch (argument)
    {
        case "first":
            first = new Rangefinder.LineSample(reference);
            break;
        case "second":
            second = new Rangefinder.LineSample(reference);

            Vector3D closestFirst, closestSecond;
            if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
            {
                // We're interested in the midpoint of the closestFirst-closestSecond segment
                var target = (closestFirst + closestSecond) / 2.0;
                rangefinderResult.Clear();
                rangefinderResult.Append(string.Format("Target: {0:F2}, {1:F2}, {2:F2}",
                                                       target.GetDim(0),
                                                       target.GetDim(1),
                                                       target.GetDim(2)));
                rangefinderResult.Append('\n');

                var targetVector = target - reference.GetPosition();
                rangefinderResult.Append(string.Format("Distance: {0} m", (ulong)(targetVector.Length() + 0.5)));
            }
            else
            {
                rangefinderResult.Clear();
                rangefinderResult.Append("Parallel lines???");
            }
            break;
    }

    program.Echo(rangefinderResult.ToString());
}

private IMyCubeBlock GetReference(MyGridProgram program)
{
    var referenceGroup = ZALibrary.GetBlockGroupWithName(this, RANGEFINDER_REFERENCE_GROUP);
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: " + RANGEFINDER_REFERENCE_GROUP);
    }
    else if (referenceGroup.Blocks.Count != 1)
    {
        throw new Exception("Expecting exactly 1 block in group " + RANGEFINDER_REFERENCE_GROUP);
    }
    return referenceGroup.Blocks[0];
}
