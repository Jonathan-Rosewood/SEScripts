//! Utility Controller
//@ shipcontrol eventdriver dockingmanager safemode smartundock
//@ batterymonitor redundancy emergencystop
//@ cruisecontrol vtvlhelper damagecontrol rangefinder
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

private readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly DockingManager dockingManager = new DockingManager();
private readonly SafeMode safeMode = new SafeMode(new MySafeModeHandler());
private readonly SmartUndock smartUndock = new SmartUndock();
private readonly CruiseControl cruiseControl = new CruiseControl();
private readonly VTVLHelper vtvlHelper = new VTVLHelper();
private readonly DamageControl damageControl = new DamageControl();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

private Rangefinder.LineSample first;
private StringBuilder rangefinderResult = new StringBuilder();

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: SHIP_GROUP,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, VTVLHELPER_REMOTE_GROUP);

        dockingManager.Init(commons, eventDriver, safeMode,
                            new BatteryMonitor(),
                            new RedundancyManager());
        smartUndock.Init(commons, eventDriver);
        cruiseControl.Init(commons, eventDriver, LivenessCheck);
        vtvlHelper.Init(commons, eventDriver, LivenessCheck);
    }

    eventDriver.Tick(commons, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument, () =>
                    {
                        dockingManager.ManageShip(commons, eventDriver, false);
                    });
            cruiseControl.HandleCommand(commons, eventDriver, argument);
            vtvlHelper.HandleCommand(commons, eventDriver, argument);
            damageControl.HandleCommand(commons, eventDriver, argument);
            HandleCommand(commons, argument);
        },
        postAction: () => {
            damageControl.Display(commons);
            cruiseControl.Display(commons);
            vtvlHelper.Display(commons);
            smartUndock.Display(commons);
            Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

void HandleCommand(ZACommons commons, string argument)
{
    argument = argument.Trim().ToString();
    if (argument == "first" || argument == "second")
    {
        var reference = GetReference(commons);

        if (argument == "first")
        {
            first = new Rangefinder.LineSample(reference);
            rangefinderResult.Clear();
        }
        else if (argument == "second")
        {
            var second = new Rangefinder.LineSample(reference);

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

                RangefinderAction(commons, target);
            }
            else
            {
                rangefinderResult.Clear();
                rangefinderResult.Append("Parallel lines???");
            }
        }
    }
    else if (argument == "gfirst" || argument == "gsecond")
    {
        var reference = vtvlHelper.GetRemoteControl(commons);
        var gravity = reference.GetNaturalGravity();
        if (gravity.LengthSquared() == 0.0) return;

        if (argument == "gfirst")
        {
            first = new Rangefinder.LineSample(reference, gravity);
        }
        else if (argument == "gsecond")
        {
            var second = new Rangefinder.LineSample(reference, gravity);

            Vector3D closestFirst, closestSecond;
            if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
            {
                var center = (closestFirst + closestSecond) / 2.0;
                var radius = (reference.GetPosition() - center).Length();
                GravitySurveyAction(commons, center, radius);
            }
        }
    }
}

void RangefinderAction(ZACommons commons, Vector3D target)
{
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

void GravitySurveyAction(ZACommons commons, Vector3D center, double radius)
{
    var targetGroup = commons.GetBlockGroupWithName(VTVLHELPER_TARGET_GROUP);
    if (targetGroup != null)
    {
        var targetString = string.Format("{0};{1};{2};{3}",
                                         center.GetDim(0),
                                         center.GetDim(1),
                                         center.GetDim(2),
                                         radius);

        for (var e = ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
        {
            ((IMyTextPanel)e.Current).WritePublicText(targetString);
        }
    }
}

bool LivenessCheck(ZACommons commons, EventDriver eventDriver)
{
    if (CONTROL_CHECK_ENABLED) safeMode.TriggerIfUncontrolled(commons, eventDriver);
    return !safeMode.Abandoned;
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

private void Display(ZACommons commons)
{
    commons.Echo(rangefinderResult.ToString());
}
