//@ shipcontrol eventdriver seeker
public class LOSGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Vector3D LauncherReferencePoint;
    private Vector3D LauncherReferenceDirection;

    private bool Disconnected = false;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        eventDriver.Schedule(0, Run);
        eventDriver.Schedule(FULL_BURN_DELAY, FullBurn);
    }

    public void FullBurn(ZACommons commons, EventDriver eventDriver)
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
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        Vector3D? velocity = shipControl.LinearVelocity;
        if (velocity != null)
        {
            // Vector from launcher to missile
            var launcherVector = shipControl.ReferencePoint - LauncherReferencePoint;
            // Determine projection on launcher direction vector
            var launcherDot = launcherVector.Dot(LauncherReferenceDirection);
            var launcherProj = launcherDot * LauncherReferenceDirection;
            Vector3D prediction;
            if (launcherDot >= 0.0)
            {
                // Set prediction to that projection
                prediction = launcherProj;
            }
            else
            {
                // Or set it to the launcher (0,0,0) if the missile is
                // behind the launcher
                prediction = new Vector3D(0, 0, 0);
            }

            // Offset forward by some amount
            prediction += LEAD_DISTANCE * LauncherReferenceDirection;

            // Offset by launcher position
            prediction += LauncherReferencePoint;

            // Determine relative vector to aim point
            var targetVector = prediction - shipControl.ReferencePoint;
            // Project onto our velocity
            var velocityNorm = Vector3D.Normalize((Vector3D)velocity);
            var forwardProj = velocityNorm * Vector3D.Dot(targetVector, velocityNorm);
            // Use scaled rejection for oversteer
            var forwardRej = (targetVector - forwardProj) * OVERSTEER_FACTOR;
            // Add to projection to get adjusted aimpoint
            var aimPoint = forwardProj + forwardRej;

            double yawError, pitchError;
            seeker.Seek(shipControl, aimPoint, out yawError, out pitchError);
        }
        else
        {
            // Can't really do crap w/o our own velocity
            shipControl.GyroControl.Reset();
        }

        eventDriver.Schedule(FramesPerRun, Run);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        if (Disconnected) return;

        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        if (parts.Length == 2)
        {
            if (parts[0] == "disconnect" && MissileGroupSuffix.Equals(parts[1], ZACommons.IGNORE_CASE))
            {
                Disconnected = true;
            }
            return;
        }
        if (parts.Length != 7) return;
        if (parts[0] != "bupdate") return;
        LauncherReferencePoint = new Vector3D();
        for (int i = 1; i < 4; i++)
        {
            LauncherReferencePoint.SetDim(i-1, double.Parse(parts[i]));
        }
        LauncherReferenceDirection = new Vector3D();
        for (int i = 4; i < 7; i++)
        {
            LauncherReferenceDirection.SetDim(i-4, double.Parse(parts[i]));
        }
    }
}
