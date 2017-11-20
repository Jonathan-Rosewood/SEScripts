//@ shipcontrol eventdriver
public class ProNavGuidance
{
    private const uint FramesPerRun = 1;

    private Vector3D Target, TargetVelocity;
    private TimeSpan LastTargetUpdate;

    // Acquire target data from launcher
    public void AcquireTarget(ZACommons commons, EventDriver eventDriver)
    {
        // Find the sole text panel
        var panelGroup = commons.GetBlockGroupWithName("CM Target");
        if (panelGroup == null)
        {
            throw new Exception("Missing group: CM Target");
        }

        var panels = ZACommons.GetBlocksOfType<IMyTextPanel>(panelGroup.Blocks);
        if (panels.Count == 0)
        {
            throw new Exception("Expecting at least 1 text panel");
        }
        var panel = panels[0]; // Just use the first one
        var targetString = panel.GetPublicText();

        // Parse target info
        var parts = targetString.Split(';');
        if (parts.Length != 6)
        {
            throw new Exception("Expecting exactly 6 parts to target info");
        }
        Target = new Vector3D();
        for (int i = 0; i < 3; i++)
        {
            Target.SetDim(i, double.Parse(parts[i]));
        }
        TargetVelocity = new Vector3D();
        for (int i = 3; i < 6; i++)
        {
            TargetVelocity.SetDim(i-3, double.Parse(parts[i]));
        }
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        // Full burn
        var thrustControl = shipControl.ThrustControl;
        thrustControl.SetOverride(Base6Directions.Direction.Forward, true);
        // And disable thrusters in all other directions
        thrustControl.Enable(Base6Directions.Direction.Backward, false);
        thrustControl.Enable(Base6Directions.Direction.Up, false);
        thrustControl.Enable(Base6Directions.Direction.Down, false);
        thrustControl.Enable(Base6Directions.Direction.Left, false);
        thrustControl.Enable(Base6Directions.Direction.Right, false);

        // Reset gyro
        var gyroControl = shipControl.GyroControl;
        gyroControl.Reset();

        eventDriver.Schedule(0, Run);
    }

    private float ClampRPM(float value)
    {
        return Math.Max(-PN_MAX_GYRO_RPM, Math.Min(PN_MAX_GYRO_RPM, value));
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        var gyroControl = shipControl.GyroControl;

        Vector3D? velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Interpolate position since last update
            var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
            var TargetGuess = Target + TargetVelocity * delta.TotalSeconds;

            // Do PN
            var offset = TargetGuess - shipControl.ReferencePoint;
            var relativeVelocity = TargetVelocity - (Vector3D)velocity;
            var omega = offset.Cross(relativeVelocity) / offset.Dot(offset);
            var direction = Vector3D.Normalize((Vector3D)velocity);
            var accel = Vector3D.Cross(direction * -PN_GUIDANCE_GAIN * relativeVelocity.Length(), omega);

            // Translate into local space
            var toLocal = MatrixD.Invert(MatrixD.CreateWorld(Vector3D.Zero, shipControl.ReferenceForward, shipControl.ReferenceUp));
            var localAccel = Vector3D.Transform(-accel, toLocal);

            var yaw = ClampRPM((float)localAccel.X);
            var pitch = ClampRPM((float)localAccel.Y);

            // Actuate the gyros
            gyroControl.SetAxisVelocityRPM(GyroControl.Yaw, yaw);
            gyroControl.SetAxisVelocityRPM(GyroControl.Pitch, pitch);
        }
        else
        {
            // Can't really do crap w/o our own velocity
            gyroControl.Reset();
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }

    // A remote update via antenna
    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        if (parts.Length != 7) return;
        if (parts[0] != "tupdate") return;
        Target = new Vector3D();
        for (int i = 1; i < 4; i++)
        {
            Target.SetDim(i-1, double.Parse(parts[i]));
        }
        TargetVelocity = new Vector3D();
        for (int i = 4; i < 7; i++)
        {
            TargetVelocity.SetDim(i-4, double.Parse(parts[i]));
        }
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }

    // An update from e.g. an onboard seeker
    public void UpdateTarget(EventDriver eventDriver, Vector3D target, Vector3D velocity)
    {
        Target = target;
        TargetVelocity = velocity;
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }
}
