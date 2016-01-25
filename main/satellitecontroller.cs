public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
private readonly SafeMode safeMode = new SafeMode();
private readonly RedundancyManager redundancyManager = new RedundancyManager();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        safeMode.Init(commons, eventDriver);
        redundancyManager.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons);
}
