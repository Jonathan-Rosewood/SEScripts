//! Turret Tracker
//@ shipcontrol eventdriver customdata rotorstepper
private readonly EventDriver eventDriver = new EventDriver();
private readonly TurretTracker turretTracker = new TurretTracker();
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

        shipOrientation.SetShipReference<IMyShipController>(commons.Blocks);

        turretTracker.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            turretTracker.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            turretTracker.Display(commons);
        });

    if (commons.IsDirty) Storage = myStorage.Encode();
}

public class TurretTracker
{
    enum States { Idle, Free, Tracking };

    private States State = States.Idle;

    private Vector3D TurretDirection;

    private readonly RotorStepper AzimuthRotor = new RotorStepper(TURRET_TRACKER_AZIMUTH_GROUP);
    private readonly RotorStepper ElevationRotor = new RotorStepper(TURRET_TRACKER_ELEVATION_GROUP);
    private double AzimuthSign = 1.0, ElevationSign = 1.0;

    private MyDetectedEntityInfo TargetInfo;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.ToLower().Trim();
        switch (argument)
        {
            case "enable":
                {
                    if (State == States.Idle)
                    {
                        State = States.Free;
                        eventDriver.Schedule(0, FreeTrack);
                    }
                    break;
                }
            case "disable":
                {
                    State = States.Idle;
                    break;
                }
        }
    }

    public void FreeTrack(ZACommons commons, EventDriver eventDriver)
    {
        if (State == States.Idle) return;

        var turret = GetTurret(commons);

        Vector3D direction;
        Vector3D.CreateFromAzimuthAndElevation(turret.Azimuth, turret.Elevation, out direction);
        TurretDirection = Vector3D.TransformNormal(direction, turret.WorldMatrix);

        AimRotorAtPoint(commons, eventDriver, AzimuthRotor, TurretDirection, AzimuthSign);
        AimRotorAtPoint(commons, eventDriver, ElevationRotor, TurretDirection, ElevationSign);

        TargetInfo = turret.GetTargetedEntity();

        eventDriver.Schedule(1, FreeTrack);
    }

    public void Display(ZACommons commons)
    {
        commons.Echo(string.Format("Status: {0}", State));
        commons.Echo(string.Format("TargetInfo: {0}", !TargetInfo.IsEmpty()));
    }

    private void AimRotorAtPoint(ZACommons commons, EventDriver eventDriver, RotorStepper stepper, Vector3D point, double sign)
    {
        var rotor = stepper.GetRotor(commons);
        var offset = point;
        // Convert to rotor's coordinate space
        offset = Vector3D.TransformNormal(offset, MatrixD.Transpose(rotor.WorldMatrix));
        // Why you backwards, Atan2?? Or rather, Keen's world coordinates...
        var desiredAngle = Math.Atan2(-offset.X, offset.Z);

        stepper.SetPoint = desiredAngle * sign;
        stepper.Schedule(eventDriver);
    }

    private IMyLargeTurretBase GetTurret(ZACommons commons)
    {
        var group = commons.GetBlockGroupWithName(TURRET_TRACKER_TURRET_GROUP);
        if (group == null) throw new Exception("Missing group: " + TURRET_TRACKER_TURRET_GROUP);
        var turrets = ZACommons.GetBlocksOfType<IMyLargeTurretBase>(group.Blocks, turret => turret.IsFunctional && turret.Enabled);
        if (turrets.Count < 1) throw new Exception("Missing turret in group " + TURRET_TRACKER_TURRET_GROUP);
        return turrets[0];
    }
}
