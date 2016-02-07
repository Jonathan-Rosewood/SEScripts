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

        eventDriver.Schedule(0.0, Run);
    }

    eventDriver.Tick(commons);
}

public void Run(ZACommons commons, EventDriver eventDriver)
{
    // Disable all reactors (except our own) on all connected grids
    // If we're fully solar-powered, we don't want to waste uranium on
    // docked ships.
    var reactors = ZACommons.GetBlocksOfType<IMyReactor>(commons.AllBlocks,
                                                         block => block.CubeGrid != commons.Me.CubeGrid);
    reactors.ForEach(block => block.SetValue<bool>("OnOff", false));

    eventDriver.Schedule(5.0, Run);
}
