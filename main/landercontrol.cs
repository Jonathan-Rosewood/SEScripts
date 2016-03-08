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
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation,
                                         shipGroup: SHIP_NAME,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "Autopilot Reference");

        dockingManager.Init(commons, eventDriver, safeMode,
                            new RedundancyManager());
        smartUndock.Init(commons);
        cruiseControl.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            safeMode.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument, () =>
                    {
                        dockingManager.ManageShip(commons, eventDriver, false);
                    });
            cruiseControl.HandleCommand(commons, eventDriver, argument);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
