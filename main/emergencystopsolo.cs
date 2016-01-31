public readonly EventDriver eventDriver = new EventDriver(timerName: STANDARD_LOOP_TIMER_BLOCK_NAME);
public readonly EmergencyStop emergencyStop = new EmergencyStop();

private readonly ShipOrientation shipOrientation = new ShipOrientation();

private bool FirstRun = true;

void Main(string argument)
{
    var commons = new ShipControlCommons(this, shipOrientation);

    if (FirstRun)
    {
        FirstRun = false;

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);
    }

    eventDriver.Tick(commons, preAction: () =>
            {
                HandleCommand(commons, eventDriver, argument);
            });
}

public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                          string argument)
{
    argument = argument.Trim().ToLower();
    if (argument == "stop")
    {
        emergencyStop.SafeMode(commons, eventDriver);
    }
}
