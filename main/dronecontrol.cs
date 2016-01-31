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
public readonly DockingManager dockingManager = new DockingManager(new SafeMode(new MySafeModeHandler()), new BatteryMonitor(), new RedundancyManager());
public readonly SmartUndock smartUndock = new SmartUndock();
public readonly ZAStorage myStorage = new ZAStorage();

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

        dockingManager.Init(commons, eventDriver);
        smartUndock.Init(commons);
    }

    eventDriver.Tick(commons, preAction: () => {
            dockingManager.HandleCommand(commons, eventDriver, argument);
            smartUndock.HandleCommand(commons, eventDriver, argument, () =>
                    {
                        dockingManager.ManageShip(commons, eventDriver, false);
                    });
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}
