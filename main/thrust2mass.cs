//! Thrust to Mass
//@ shipcontrol eventdriver customdata
private readonly EventDriver eventDriver = new EventDriver();
private readonly ZAStorage myStorage = new ZAStorage();

private readonly ShipOrientation shipOrientation = new ShipOrientation();
private readonly ZACustomData customData = new ZACustomData();

private bool FirstRun = true;

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ShipControlCommons(this, updateType, shipOrientation,
                                         storage: myStorage);

    if (FirstRun)
    {
        FirstRun = false;

        customData.Parse(Me);

        myStorage.Decode(Storage);

        shipOrientation.SetShipReference(commons, "CruiseControlReference");
    }

    eventDriver.Tick(commons, argAction: () => {
        },
        postAction: () => {
            Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

float GetMaxThrust(ShipControlCommons shipControl, Base6Directions.Direction direction)
{
    float maxThrust = 0.0f;
    foreach (var thruster in shipControl.ThrustControl.GetThrusters(direction))
    {
        maxThrust += thruster.MaxEffectiveThrust;
    }
    return maxThrust;
}

void Display(ZACommons commons)
{
    var shipControl = (ShipControlCommons)commons;

    var mass = shipControl.ShipController.CalculateShipMass().PhysicalMass;

    commons.Echo(string.Format("Mass: {0:F3}", mass));
    float thrust;
    thrust = GetMaxThrust(shipControl, Base6Directions.Direction.Forward);
    commons.Echo(string.Format("Forward: {0:F1} ({1:F3})", thrust, thrust / mass));
    thrust = GetMaxThrust(shipControl, Base6Directions.Direction.Backward);
    commons.Echo(string.Format("Backward: {0:F1} ({1:F3})", thrust, thrust / mass));
    thrust = GetMaxThrust(shipControl, Base6Directions.Direction.Up);
    commons.Echo(string.Format("Up: {0:F1} ({1:F3})", thrust, thrust / mass));
    thrust = GetMaxThrust(shipControl, Base6Directions.Direction.Down);
    commons.Echo(string.Format("Down: {0:F1} ({1:F3})", thrust, thrust / mass));
    thrust = GetMaxThrust(shipControl, Base6Directions.Direction.Left);
    commons.Echo(string.Format("Left: {0:F1} ({1:F3})", thrust, thrust / mass));
    thrust = GetMaxThrust(shipControl, Base6Directions.Direction.Right);
    commons.Echo(string.Format("Right: {0:F1} ({1:F3})", thrust, thrust / mass));
}
