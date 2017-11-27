//! Target Tracker
//@ shipcontrol eventdriver targettracker
private readonly EventDriver eventDriver = new EventDriver();
private readonly TargetTracker targetTracker = new TargetTracker();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference(commons, MAIN_CAMERA_GROUP);

        targetTracker.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            targetTracker.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            targetTracker.Display(commons, eventDriver);
        });
}
