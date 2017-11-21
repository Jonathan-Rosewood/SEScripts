//@ shipcontrol eventdriver seeker
public class LOSGuidance
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private Vector3D LauncherReferencePoint;
    private Vector3D LauncherReferenceDirection;

    public void SetLauncherReference(IMyCubeBlock launcherReference,
                                     Base6Directions.Direction direction = Base6Directions.Direction.Forward)
    {
        LauncherReferencePoint = launcherReference.GetPosition();
        var forward3I = launcherReference.Position + Base6Directions.GetIntVector(launcherReference.Orientation.TransformDirection(direction));
        var forwardPoint = launcherReference.CubeGrid.GridIntegerToWorld(forward3I);
        LauncherReferenceDirection = Vector3D.Normalize(forwardPoint - LauncherReferencePoint);
    }

    public IMyTerminalBlock SetLauncherReference(ZACommons commons, string groupName,
                                                 Base6Directions.Direction direction = Base6Directions.Direction.Forward,
                                                 Func<IMyTerminalBlock, bool> condition = null)
    {
        var group = commons.GetBlockGroupWithName(groupName);
        if (group != null)
        {
            foreach (var block in group.Blocks)
            {
                if (condition == null || condition(block))
                {
                    // Use first block that matches condition
                    SetLauncherReference(block, direction);
                    return block;
                }
            }
        }
        throw new Exception("Cannot set launcher reference from group: " + groupName);
    }

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
}
