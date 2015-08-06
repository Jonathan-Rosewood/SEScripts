public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly DockingManager dockingManager = new DockingManager();
public readonly SafeMode safeMode = new SafeMode();
public readonly BatteryMonitor batteryMonitor = new BatteryMonitor();
public readonly SolarRotorController rotorController = new SolarRotorController();

private Rangefinder.LineSample first, second;
private StringBuilder rangefinderResult = new StringBuilder();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ZACommons(this);

    if (FirstRun)
    {
        FirstRun = true;
        eventDriver.Schedule(0.0);
    }

    dockingManager.HandleCommand(commons, argument);
    HandleCommand(commons, argument);

    eventDriver.Tick(commons, () =>
            {
                // This really seems like it should be determined once per run
                var isConnected = ZACommons.IsConnectedAnywhere(commons.Blocks);

                dockingManager.Run(commons, isConnected);
                safeMode.Run(commons, isConnected);
                batteryMonitor.Run(commons, isConnected);
                if (MAX_POWER_ENABLED) rotorController.Run(commons);

                eventDriver.Schedule(1.0);
            });
}

private void HandleCommand(ZACommons commons, string argument)
{
    var reference = GetReference(commons);

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

    commons.Echo(rangefinderResult.ToString());
}

private IMyCubeBlock GetReference(ZACommons commons)
{
    var referenceGroup = commons.GetBlockGroupWithName(RANGEFINDER_REFERENCE_GROUP);
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: " + RANGEFINDER_REFERENCE_GROUP);
    }
    return referenceGroup.Blocks[0];
}