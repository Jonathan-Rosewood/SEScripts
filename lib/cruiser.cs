//@ shipcontrol eventdriver pid
public class Cruiser
{
    private const double ThrustKp = 1.0;
    private const double ThrustKi = 0.001;
    private const double ThrustKd = 1.0;
    private readonly PIDController thrustPID;
    private readonly double ThrustDeadZone;

    public Base6Directions.Direction LocalForward { get; private set; }
    public Base6Directions.Direction LocalBackward { get; private set; }

    public Cruiser(double dt, double thrustDeadZone)
    {
        thrustPID = new PIDController(dt);
        thrustPID.Kp = ThrustKp;
        thrustPID.Ki = ThrustKi;
        thrustPID.Kd = ThrustKd;
        ThrustDeadZone = thrustDeadZone;
    }

    public void Init(ShipControlCommons shipControl,
                     Base6Directions.Direction localForward = Base6Directions.Direction.Forward)
    {
        LocalForward = localForward;
        LocalBackward = Base6Directions.GetFlippedDirection(LocalForward);

        thrustPID.Reset();
    }

    // Use ship controller velocity
    public bool Cruise(ShipControlCommons shipControl, EventDriver eventDriver,
                       double targetSpeed,
                       Func<IMyThrust, bool> condition = null,
                       bool enableForward = true,
                       bool enableBackward = true)
    {
        var velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            Cruise(shipControl, targetSpeed, (Vector3D)velocity, condition,
                   enableForward, enableBackward);
        }
        return velocity != null;
    }

    // Use externally-measured velocity
    public void Cruise(ShipControlCommons shipControl, double targetSpeed,
                       Vector3D velocity,
                       Func<IMyThrust, bool> condition = null,
                       bool enableForward = true,
                       bool enableBackward = true)
    {
        // Determine forward unit vector
        var forward3I = shipControl.Reference.Position + Base6Directions.GetIntVector(shipControl.ShipBlockOrientation.TransformDirection(LocalForward));
        var referenceForward = Vector3D.Normalize(shipControl.Reference.CubeGrid.GridIntegerToWorld(forward3I) - shipControl.ReferencePoint);

        // Take dot product with forward unit vector
        var speed = Vector3D.Dot(velocity, referenceForward);
        var error = targetSpeed - speed;
        //shipControl.Echo(string.Format("Set Speed: {0:F1} m/s", targetSpeed));
        //shipControl.Echo(string.Format("Actual Speed: {0:F1} m/s", speed));
        //shipControl.Echo(string.Format("Error: {0:F1} m/s", error));

        var force = thrustPID.Compute(error);

        var thrustControl = shipControl.ThrustControl;
        if (Math.Abs(error) < ThrustDeadZone * targetSpeed)
        {
            // Close enough, just disable both sets of thrusters
            thrustControl.Enable(LocalForward, false, condition);
            thrustControl.Enable(LocalBackward, false, condition);
        }
        else if (force > 0.0)
        {
            // Thrust forward
            thrustControl.Enable(LocalForward, enableForward, condition);
            if (enableForward) thrustControl.SetOverride(LocalForward, force, condition);
            thrustControl.Enable(LocalBackward, false, condition);
        }
        else
        {
            thrustControl.Enable(LocalForward, false, condition);
            thrustControl.Enable(LocalBackward, enableBackward, condition);
            if (enableBackward) thrustControl.SetOverride(LocalBackward, -force, condition);
        }
    }
}
