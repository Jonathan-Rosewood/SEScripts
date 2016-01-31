public class MySafeModeHandler : SafeModeHandler
{
    public void SafeMode(ZACommons commons, EventDriver eventDriver)
    {
        // Check after 1 second (let timer block's action take effect)
        eventDriver.Schedule(1.0, (c,ed) =>
                {
                    new EmergencyStop().SafeMode(c, ed);
                });
    }
}

public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly DockingManager dockingManager = new DockingManager();
public readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
public readonly SmartUndock smartUndock = new SmartUndock();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private Rangefinder.LineSample first, second;
private StringBuilder rangefinderResult = new StringBuilder();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, RANGEFINDER_REFERENCE_GROUP);

        dockingManager.Init(commons, eventDriver, safeMode,
                            new BatteryMonitor(),
                            new RedundancyManager());
        smartUndock.Init(commons);
    }

    eventDriver.Tick(commons, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument, () =>
                    {
                        dockingManager.ManageShip(commons, eventDriver, false);
                    });
            HandleCommand(commons, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

public void HandleCommand(ZACommons commons, string argument)
{
    argument = argument.Trim().ToLower();
    switch (argument)
    {
        case "first":
            first = new Rangefinder.LineSample(GetReference(commons));
            break;
        case "second":
            var reference = GetReference(commons);
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

                // Also output to text panel group, if present
                var targetGroup = commons.GetBlockGroupWithName(RANGEFINDER_TARGET_GROUP);
                if (targetGroup != null)
                {
                    var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                                     target.GetDim(0),
                                                     target.GetDim(1),
                                                     target.GetDim(2));

                    for (var e = ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
                    {
                        ((IMyTextPanel)e.Current).WritePublicText(targetString);
                    }
                }
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
    var references = ZACommons.GetBlocksOfType<IMyTerminalBlock>(referenceGroup.Blocks,
                                                                 block => block.CubeGrid == commons.Me.CubeGrid);
    if (references.Count == 0)
    {
        throw new Exception("Expecting at least 1 block on the same grid: " + RANGEFINDER_REFERENCE_GROUP);
    }
    return references[0];
}
