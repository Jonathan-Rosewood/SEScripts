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
        smartUndock.Init(commons);
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
            HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
{
    argument = argument.Trim().ToString();
    if (argument == "first" || argument == "second")
    {
        var reference = vtvlHelper.GetRemoteControl(commons);
        var gravity = reference.GetNaturalGravity();
        if (gravity.LengthSquared() == 0.0) return;

        if (argument == "first")
        {
            first = new Rangefinder.LineSample(reference, gravity);
        }
        else if (argument == "second")
        {
            var second = new Rangefinder.LineSample(reference, gravity);

            Vector3D closestFirst, closestSecond;
            if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
            {
                var center = (closestFirst + closestSecond) / 2.0;
                var radius = (reference.GetPosition() - center).Length();
                TargetAction(commons, center, radius);
            }
        }
    }
}

void TargetAction(ZACommons commons, Vector3D target, double radius)
{
    var targetGroup = commons.GetBlockGroupWithName(VTVLHELPER_TARGET_GROUP);
    if (targetGroup != null)
    {
        var targetString = string.Format("{0};{1};{2};{3}",
                                         target.GetDim(0),
                                         target.GetDim(1),
                                         target.GetDim(2),
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
