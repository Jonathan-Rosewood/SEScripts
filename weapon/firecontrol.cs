//@ shipcontrol eventdriver quadraticsolver customdata
public class FireControl
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private const int IDLE = 0;
    private const int ARMED = 1;
    private const int SNAPSHOT = 2;
    private const int LOCKED = 3;

    private int Mode = IDLE;

    private long TargetID;
    private Vector3D TargetOffset, TargetAimPoint, TargetVelocity;
    private TimeSpan LastTargetUpdate;

    // Shell parameters
    private double ShellSpeed, ShellFireDelay, ShellOffset;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);
        seeker.ControlThreshold = 0.0;

        // Get things into a known state
        var camera = GetSightingCamera(commons);
        if (camera != null) camera.EnableRaycast = false;
        Mode = IDLE;

        shipControl.GyroControl.EnableOverride(false);

        // In case we were loaded in with the shell already built
        GetShellParameters(commons);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        switch (argument)
        {
            case "arm":
                if (Mode == IDLE)
                {
                    var camera = GetSightingCamera(commons);
                    if (camera != null)
                    {
                        camera.EnableRaycast = true;
                        Mode = ARMED;
                    }
                }
                break;
            case "disarm":
                {
                    var camera = GetSightingCamera(commons);
                    if (camera != null) camera.EnableRaycast = false;
                    MaybeEndLock(commons, eventDriver);
                    Mode = IDLE;
                    break;
                }
            case "lock":
                {
                    BeginSnapshot(commons, eventDriver);
                    break;
                }
            case "unlock":
                {
                    if (Mode != IDLE)
                    {
                        MaybeEndLock(commons, eventDriver);
                        Mode = ARMED;
                    }
                    break;
                }
            case "updateshell":
                {
                    GetShellParameters(commons);
                    break;
                }
            case "firefirefire":
                {
                    BeginFiring(commons, eventDriver);
                    break;
                }
            default:
                if (Mode == LOCKED)
                {
                    HandleRemoteCommand(commons, eventDriver, argument);
                }
                break;
        }
    }

    // Copy/pasted/edited from basmissileguidance. Refactor?
    public void HandleRemoteCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        var parts = argument.Split(';');
        switch (parts[0])
        {
            case "tupdate":
                {
                    if (parts.Length != 12) return;
                    var targetID = long.Parse(parts[1]);
                    // Ignore if it's an update for another target
                    if (targetID != TargetID) return;
                    break;
                }
            default:
                return;
        }
        var targetPosition = new Vector3D();
        for (int i = 2; i < 5; i++)
        {
            targetPosition.SetDim(i-2, double.Parse(parts[i]));
        }
        TargetVelocity = new Vector3D();
        for (int i = 5; i < 8; i++)
        {
            TargetVelocity.SetDim(i-5, double.Parse(parts[i]));
        }
        var orientation = new QuaternionD();
        orientation.X = double.Parse(parts[8]);
        orientation.Y = double.Parse(parts[9]);
        orientation.Z = double.Parse(parts[10]);
        orientation.W = double.Parse(parts[11]);
        var targetOrientation = MatrixD.CreateFromQuaternion(orientation);

        // Re-derive aim point
        TargetAimPoint = targetPosition + Vector3D.Transform(TargetOffset, targetOrientation);
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }

    private void BeginSnapshot(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode == IDLE)
        {
            var camera = GetSightingCamera(commons);
            if (camera == null) return;
            camera.EnableRaycast = true;
        }

        if (Mode != SNAPSHOT) eventDriver.Schedule(1, Snapshot);
        MaybeEndLock(commons, eventDriver);
        Mode = SNAPSHOT;

        GetShellParameters(commons);
    }

    public void Snapshot(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != SNAPSHOT) return;

        var camera = GetSightingCamera(commons);
        if (camera == null)
        {
            Mode = IDLE;
            return;
        }

        // Can we raycast the desired distance?
        var scanTime = camera.TimeUntilScan(FC_INITIAL_RAYCAST_RANGE);
        if (scanTime > 0)
        {
            // Try later
            eventDriver.Schedule((double)scanTime / 1000.0, Snapshot);
            return;
        }

        var info = camera.Raycast(FC_INITIAL_RAYCAST_RANGE);
        if (info.IsEmpty())
        {
            // Missed? Try again
            eventDriver.Schedule(1, Snapshot);
            return;
        }

        TargetAimPoint = (Vector3D)info.HitPosition;
        TargetVelocity = new Vector3D(info.Velocity);
        LastTargetUpdate = eventDriver.TimeSinceStart;

        TargetID = info.EntityId;

        // Determine local offset of aim point, in case we get an update
        var offset = TargetAimPoint - info.Position;
        var toLocal = MatrixD.Invert(info.Orientation);
        TargetOffset = Vector3D.Transform(offset, toLocal);

        BeginLock(commons, eventDriver);
    }

    private void BeginLock(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        shipControl.GyroControl.EnableOverride(true);
        eventDriver.Schedule(1, Lock);
        Mode = LOCKED;
    }

    public void Lock(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != LOCKED) return;

        var shipControl = (ShipControlCommons)commons;

        // Guesstimate current target position
        var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
        var targetGuess = TargetAimPoint + TargetVelocity * delta.TotalSeconds;

        // Solve for intersection of expanding sphere + moving object
        // Note sphere may have an initial radius (to account for offset from
        // CoM) and may only start expanding after some delay (to account for
        // firing sequence + acceleration).
        var offset = targetGuess - shipControl.ReferencePoint;
        var tVelSquared = Vector3D.Dot(TargetVelocity, TargetVelocity);
        var a = tVelSquared - ShellSpeed * ShellSpeed;
        var offsetDotVel = Vector3D.Dot(offset, TargetVelocity);
        var b = 2.0 * (tVelSquared * ShellFireDelay + offsetDotVel - ShellOffset * ShellSpeed);
        var c = Vector3D.Dot(offset, offset) + ShellFireDelay * ShellFireDelay * tVelSquared + 2.0 * ShellFireDelay * offsetDotVel - ShellOffset * ShellOffset;

        double interceptTime = 0.0;

        double s1, s2;
        int solutions = QuadraticSolver.Solve(a, b, c, out s1, out s2);
        // Pick smallest positive intercept time
        if (solutions == 1)
        {
            if (s1 > 0.0) interceptTime = s1;
        }
        else if (solutions == 2)
        {
            if (s1 > 0.0) interceptTime = s1;
            else if (s2 > 0.0) interceptTime = s2;
        }

        var prediction = targetGuess + TargetVelocity * interceptTime;
        
        double yawPitchError;
        seeker.Seek(shipControl, prediction - shipControl.ReferencePoint, out yawPitchError);

        eventDriver.Schedule(1, Lock);
    }

    public void Display(ZACommons commons, EventDriver eventDriver)
    {
        switch (Mode)
        {
            case IDLE:
                commons.Echo("Fire Control: Off");
                break;
            case ARMED:
                commons.Echo("Fire Control: Enabled");
                break;
            case SNAPSHOT:
                commons.Echo("Fire Control: Searching");
                break;
            case LOCKED:
                commons.Echo("Fire Control: Locked");
                commons.Echo(string.Format("Last Update: {0:F1} s", (eventDriver.TimeSinceStart - LastTargetUpdate).TotalSeconds));
                break;
        }
        commons.Echo(string.Format("Shell Speed: {0:F1} m/s", ShellSpeed));
        commons.Echo(string.Format("Shell Offset: {0:F2} m", ShellOffset));
        commons.Echo(string.Format("Fire Delay: {0:F2} s", ShellFireDelay));
    }

    private void BeginFiring(ZACommons commons, EventDriver eventDriver)
    {
        var group = commons.GetBlockGroupWithName(FC_FIRE_GROUP);
        if (group != null)
        {
            foreach (var block in group.Blocks)
            {
                if (block is IMyProgrammableBlock)
                {
                    ((IMyProgrammableBlock)block).TryRun("firefirefire");
                }
            }
        }

        if (Mode == LOCKED)
        {
            // Zero out gyro and keep it overridden
            var shipControl = (ShipControlCommons)commons;
            shipControl.GyroControl.Reset();
            Mode = ARMED;
            eventDriver.Schedule(3.0, EndLock);
        }
    }

    private void EndLock(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        shipControl.GyroControl.EnableOverride(false);
    }

    // This is needed (instead of disabling override in Lock) because of
    // the delayed unlock above.
    private void MaybeEndLock(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode == LOCKED) EndLock(commons, eventDriver);
    }

    private IMyCameraBlock GetSightingCamera(ZACommons commons)
    {
        var group = commons.GetBlockGroupWithName(FC_MAIN_CAMERA_GROUP);
        if (group == null) return null;
        if (group.Blocks.Count != 1) return null;
        var camera = group.Blocks[0] as IMyCameraBlock;
        if (camera == null) return null;
        return camera;
    }

    private void GetShellParameters(ZACommons commons)
    {
        // Some defaults
        ShellSpeed = 100.0;
        ShellFireDelay = ShellOffset = 0.0;

        var group = commons.GetBlockGroupWithName(FC_FIRE_GROUP);
        if (group != null)
        {
            foreach (var block in group.Blocks)
            {
                if (block is IMyProgrammableBlock)
                {
                    var shellCustomData = new ZACustomData();
                    shellCustomData.Parse(block);
                    ShellSpeed = shellCustomData.GetDouble("speed");
                    ShellFireDelay = shellCustomData.GetDouble("delay");
                    ShellOffset = shellCustomData.GetDouble("offset");
                    // Only bother with the 1st one for now
                    return;
                }
            }
        }
    }
}
