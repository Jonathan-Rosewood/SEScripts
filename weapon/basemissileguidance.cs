//@ commons eventdriver missilelaunch
public abstract class BaseMissileGuidance
{
    private const double RaycastRangeBuffer = 1.2;

    public bool HaveTarget { get; private set; }

    // Primary
    protected long TargetID;
    protected Vector3D TargetPosition, TargetVelocity, TargetOffset;
    protected MatrixD TargetOrientation;
    protected TimeSpan LastTargetUpdate;

    // Derived
    protected Vector3D TargetAimPoint;

    // On-board camera
    protected IMyCameraBlock LocalCamera;

    protected BaseMissileGuidance()
    {
        HaveTarget = false;
    }

    protected virtual void TargetUpdated(EventDriver eventDriver)
    {
        // Re-derive any derived properties of the target
        TargetAimPoint = TargetPosition + Vector3D.Transform(TargetOffset, TargetOrientation);
        LastTargetUpdate = eventDriver.TimeSinceStart;

        HaveTarget = true;
    }

    // An update from e.g. an onboard seeker
    public void UpdateTarget(EventDriver eventDriver, MyDetectedEntityInfo info, bool full = false)
    {
        TargetPosition = info.Position;
        TargetVelocity = new Vector3D(info.Velocity);
        TargetOrientation = info.Orientation;

        if (full)
        {
            TargetID = info.EntityId;
            var offset = (Vector3D)info.HitPosition - TargetPosition;
            var toLocal = MatrixD.Invert(info.Orientation);
            TargetOffset = Vector3D.Transform(offset, toLocal);
        }

        TargetUpdated(eventDriver);
    }

    // A remote update (antenna, another PB, etc.)
    public virtual void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        bool full = false;
        switch (parts[0])
        {
            case "tnew":
                {
                    if (parts.Length != 15) return;
                    TargetID = long.Parse(parts[1]);
                    full = true;
                    break;
                }
            case "tupdate":
                {
                    if (!HaveTarget || parts.Length != 12) return;
                    var targetID = long.Parse(parts[1]);
                    // Ignore if it's an update for another target
                    if (targetID != TargetID) return;
                    break;
                }
            default:
                return;
        }
        TargetPosition = new Vector3D();
        for (int i = 2; i < 5; i++)
        {
            TargetPosition.SetDim(i-2, double.Parse(parts[i]));
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
        TargetOrientation = MatrixD.CreateFromQuaternion(orientation);
        if (full)
        {
            TargetOffset = new Vector3D();
            for (int i = 12; i < 15; i++)
            {
                TargetOffset.SetDim(i-12, double.Parse(parts[i]));
            }
        }
        TargetUpdated(eventDriver);
    }

    // Camera stuff

    protected void InitCamera(ZACommons commons, EventDriver eventDriver)
    {
        var systemsGroup = commons.GetBlockGroupWithName(MissileLaunch.SYSTEMS_GROUP + MissileGroupSuffix);
        if (systemsGroup == null) return; // Kinda weird, but don't fret about it here
        // Just grab first camera from group, if any
        // TODO support multiple cameras for more frequent scans
        var cameras = ZACommons.GetBlocksOfType<IMyCameraBlock>(systemsGroup.Blocks, camera => camera.IsFunctional && camera.Enabled);
        if (cameras.Count > 0)
        {
            LocalCamera = cameras[0];
            LocalCamera.EnableRaycast = true;
            eventDriver.Schedule(1, LocalCameraScan);
        }
    }

    public void LocalCameraScan(ZACommons commons, EventDriver eventDriver)
    {
        // Is camera still alive?
        if (!LocalCamera.IsFunctional) return;

        // Guesstimate current target position
        var delta = eventDriver.TimeSinceStart - LastTargetUpdate;
        // Note we use target's center, not aim point
        var targetGuess = TargetPosition + TargetVelocity * delta.TotalSeconds;

        // Use range + buffer as raycast range
        var origin = LocalCamera.GetPosition();
        var raycastOffset = (targetGuess - origin) * RaycastRangeBuffer;
        var raycastRange = raycastOffset.Length();

        // Can we raycast the desired distance?
        var scanTime = LocalCamera.TimeUntilScan(raycastRange);
        if (scanTime > 0)
        {
            // Try later at recommended time
            eventDriver.Schedule((double)scanTime / 1000.0, LocalCameraScan);
            return;
        }

        var info = LocalCamera.Raycast(origin + raycastOffset);
        if (info.IsEmpty())
        {
            // Missed? Try again ASAP
            eventDriver.Schedule(0.1, LocalCameraScan);
            return;
        }

        // Only if it's the same target...
        if (info.EntityId == TargetID)
        {
            UpdateTarget(eventDriver, info);
        }

        eventDriver.Schedule(0.1, LocalCameraScan);
    }
}
