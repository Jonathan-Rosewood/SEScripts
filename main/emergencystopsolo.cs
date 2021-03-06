//! Emergency Stop Test
//@ shipcontrol eventdriver emergencystop
public readonly EventDriver eventDriver = new EventDriver();
public readonly EmergencyStop emergencyStop = new EmergencyStop();

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

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);
    }

    eventDriver.Tick(commons, argAction: () =>
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
