//@ shipcontrol eventdriver seeker
public class TargetTracker
{
    private const uint FramesPerRun = 1;
    private const double RunsPerSecond = 60.0 / FramesPerRun;

    private readonly Seeker seeker = new Seeker(1.0 / RunsPerSecond);

    private const int IDLE = 0;
    private const int ARMED = 1;
    private const int SNAPSHOT = 2;
    private const int PAINTING = 3;
    private const int RELEASED = 4;

    // State
    private int Mode = IDLE, PreviousMode;
    private bool GyroLock;
    private bool LocalOnly;

    private double RaycastRange;

    // Target data for gyro lock
    private Vector3D TargetPosition, TargetVelocity;
    private TimeSpan? LastTargetUpdate;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;
        seeker.Init(shipControl,
                    shipUp: shipControl.ShipUp,
                    shipForward: shipControl.ShipForward);

        // Get things into a known state
        var camera = GetMainCamera(commons);
        camera.EnableRaycast = false;
        Mode = IDLE;
        GyroLock = false;

        shipControl.GyroControl.EnableOverride(false);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        switch (argument)
        {
            case "arm":
                if (Mode == IDLE)
                {
                    var camera = GetMainCamera(commons);
                    camera.EnableRaycast = true;
                    Mode = ARMED;
                    RaycastRange = INITIAL_RAYCAST_RANGE;
                }
                break;
            case "disarm":
                {
                    var camera = GetMainCamera(commons);
                    camera.EnableRaycast = false;
                    Mode = IDLE;
                    break;
                }
            case "snapshot":
                BeginSnapshot(commons, eventDriver, localOnly: true);
                break;
            case "retarget":
                BeginSnapshot(commons, eventDriver, localOnly: false);
                break;
            case "paint":
                BeginPaint(commons, eventDriver, released: false);
                break;
            case "release":
                BeginPaint(commons, eventDriver, released: true);
                break;
            case "clear":
                {
                    if (Mode != IDLE)
                    {
                        Mode = ARMED;
                        RaycastRange = INITIAL_RAYCAST_RANGE;
                    }
                    break;
                }
        }
    }

    private void BeginSnapshot(ZACommons commons, EventDriver eventDriver, bool localOnly = true)
    {
        if (Mode == IDLE)
        {
            var camera = GetMainCamera(commons);
            camera.EnableRaycast = true;
            RaycastRange = INITIAL_RAYCAST_RANGE;
        }

        LocalOnly = localOnly;
        if (Mode != SNAPSHOT) eventDriver.Schedule(1, Snapshot);
        PreviousMode = Mode;
        Mode = SNAPSHOT;
    }

    public void Snapshot(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != SNAPSHOT) return;

        var camera = GetMainCamera(commons);

        // Can we raycast the desired distance?
        var scanTime = camera.TimeUntilScan(RaycastRange);
        if (scanTime > 0)
        {
            // Try later
            eventDriver.Schedule((double)scanTime / 1000.0, Snapshot);
            return;
        }

        var info = camera.Raycast(RaycastRange);
        if (info.IsEmpty())
        {
            // Missed? Increase range and try again
            RaycastRange = Math.Min(RaycastRange * RAYCAST_RANGE_BUFFER, INITIAL_RAYCAST_RANGE);
            eventDriver.Schedule(1, Snapshot);
            PostFeedback(commons, TRACKER_MISS_GROUP);
            return;
        }

        TargetUpdated(commons, eventDriver, info, full: true, localOnly: LocalOnly);

        // Switch to paint automatically (leave gyro lock alone)
        RaycastRange = (TargetPosition - camera.GetPosition()).Length() * RAYCAST_RANGE_BUFFER;
        BeginPaint(commons, eventDriver, released: PreviousMode == RELEASED);

        PostFeedback(commons, TRACKER_PING_GROUP);
    }

    private void BeginPaint(ZACommons commons, EventDriver eventDriver, bool released = false)
    {
        if (Mode == IDLE)
        {
            var camera = GetMainCamera(commons);
            camera.EnableRaycast = true;
            RaycastRange = INITIAL_RAYCAST_RANGE;
        }

        if (Mode != PAINTING && Mode != RELEASED) eventDriver.Schedule(1, Paint);
        Mode = released ? RELEASED : PAINTING;
        BeginLock(commons, eventDriver);
    }

    public void Paint(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != PAINTING && Mode != RELEASED) return;

        var camera = GetMainCamera(commons);

        // Can we raycast the desired distance?
        var scanTime = camera.TimeUntilScan(RaycastRange);
        if (scanTime > 0)
        {
            // Try later
            eventDriver.Schedule((double)scanTime / 1000.0, Paint);
            return;
        }

        MyDetectedEntityInfo info;
        if (GyroLock && LastTargetUpdate != null)
        {
            // If we're locked, attempt to cast at the predicted center
            var delta = eventDriver.TimeSinceStart - (TimeSpan)LastTargetUpdate;
            var targetGuess = TargetPosition + TargetVelocity * delta.TotalSeconds;
            info = camera.Raycast(targetGuess);
        }
        else
        {
            // Otherwise just cast straight ahead
            info = camera.Raycast(RaycastRange);
        }
        if (info.IsEmpty())
        {
            // Missed? Increase range, try again and release gyro
            RaycastRange = Math.Min(RaycastRange * RAYCAST_RANGE_BUFFER, INITIAL_RAYCAST_RANGE);
            GyroLock = false;
            eventDriver.Schedule(1, Paint);
            PostFeedback(commons, TRACKER_MISS_GROUP);
            return;
        }

        TargetUpdated(commons, eventDriver, info);

        RaycastRange = (TargetPosition - camera.GetPosition()).Length() * RAYCAST_RANGE_BUFFER;

        BeginLock(commons, eventDriver);
        eventDriver.Schedule(TRACKER_UPDATE_RATE, Paint);

        PostFeedback(commons, TRACKER_PING_GROUP);
    }

    private void BeginLock(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != PAINTING) return;

        if (!GyroLock)
        {
            var shipControl = (ShipControlCommons)commons;
            shipControl.GyroControl.EnableOverride(true);
            GyroLock = true;
            eventDriver.Schedule(1, Lock);
        }
    }

    public void Lock(ZACommons commons, EventDriver eventDriver)
    {
        var shipControl = (ShipControlCommons)commons;

        if (Mode != PAINTING || !GyroLock || LastTargetUpdate == null)
        {
            shipControl.GyroControl.EnableOverride(false);
            GyroLock = false;
            return;
        }

        // Guesstimate current target position
        var delta = eventDriver.TimeSinceStart - (TimeSpan)LastTargetUpdate;
        // Note we use target's center, not aim point
        var targetGuess = TargetPosition + TargetVelocity * delta.TotalSeconds;

        double yawPitchError;
        seeker.Seek(shipControl, targetGuess - shipControl.ReferencePoint, out yawPitchError);

        eventDriver.Schedule(1, Lock);
    }

    public void Display(ZACommons commons, EventDriver eventDriver)
    {
        switch (Mode)
        {
            case IDLE:
                commons.Echo("Tracker: Off");
                break;
            case ARMED:
                commons.Echo("Tracker: Enabled");
                break;
            case SNAPSHOT:
                commons.Echo("Tracker: Searching");
                commons.Echo(string.Format("Max. Range: {0:F2} m", RaycastRange));
                break;
            case PAINTING:
            case RELEASED:
                commons.Echo(string.Format("Tracker: Painting ({0})", Mode == PAINTING ? (GyroLock ? "locked" : "lost") : "released"));
                commons.Echo(string.Format("Max. Range: {0:F2} m", RaycastRange));
                if (LastTargetUpdate != null) commons.Echo(string.Format("Last Update: {0:F1} s", (eventDriver.TimeSinceStart - (TimeSpan)LastTargetUpdate).TotalSeconds));
                break;
        }
    }

    private void TargetUpdated(ZACommons commons, EventDriver eventDriver, MyDetectedEntityInfo info, bool full = false, bool localOnly = false)
    {
        TargetPosition = info.Position;
        TargetVelocity = new Vector3D(info.Velocity);
        // Convert to quaternion so it's more compact
        var orientation = QuaternionD.CreateFromRotationMatrix(info.Orientation);
        LastTargetUpdate = eventDriver.TimeSinceStart;

        // Compose message
        string msg;
        if (full)
        {
            var offset = (Vector3D)info.HitPosition - TargetPosition;
            var toLocal = MatrixD.Invert(info.Orientation);
            var localOffset = Vector3D.Transform(offset, toLocal);

            msg = string.Format("tnew;{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13}",
                                info.EntityId,
                                TargetPosition.X, TargetPosition.Y, TargetPosition.Z,
                                TargetVelocity.X, TargetVelocity.Y, TargetVelocity.Z,
                                orientation.X, orientation.Y, orientation.Z, orientation.W,
                                localOffset.X, localOffset.Y, localOffset.Z);
        }
        else
        {
            msg = string.Format("tupdate;{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}",
                                info.EntityId,
                                TargetPosition.X, TargetPosition.Y, TargetPosition.Z,
                                TargetVelocity.X, TargetVelocity.Y, TargetVelocity.Z,
                                orientation.X, orientation.Y, orientation.Z, orientation.W);
        }

        var updateGroup = commons.GetBlockGroupWithName(TARGET_UPDATE_GROUP);
        if (updateGroup != null)
        {
            var broadcasted = false;
            foreach (var block in updateGroup.Blocks)
            {
                if (block is IMyProgrammableBlock)
                {
                    ((IMyProgrammableBlock)block).TryRun(msg);
                }
                else if (!localOnly && block is IMyLaserAntenna)
                {
                    ((IMyLaserAntenna)block).TransmitMessage(msg);
                }
                else if (!localOnly && !broadcasted && block is IMyRadioAntenna)
                {
                    // Only if functional and enabled
                    var antenna = (IMyRadioAntenna)block;
                    if (antenna.IsFunctional && antenna.Enabled)
                    {
                        antenna.TransmitMessage(msg, TRACKER_ANTENNA_TARGET);
                        broadcasted = true;
                    }
                }
            }
        }
    }

    private IMyCameraBlock GetMainCamera(ZACommons commons)
    {
        var group = commons.GetBlockGroupWithName(MAIN_CAMERA_GROUP);
        if (group == null)
        {
            throw new Exception("Group missing: " + MAIN_CAMERA_GROUP);
        }
        if (group.Blocks.Count != 1)
        {
            throw new Exception("Expecting exactly 1 block in group " + MAIN_CAMERA_GROUP);
        }
        var camera = group.Blocks[0] as IMyCameraBlock;
        if (camera == null)
        {
            throw new Exception("Expecting camera in group " + MAIN_CAMERA_GROUP);
        }
        return camera;
    }

    private void PostFeedback(ZACommons commons, string groupName)
    {
        var group = commons.GetBlockGroupWithName(groupName);
        if (group != null)
        {
            foreach (var block in group.Blocks)
            {
                if (block is IMyTimerBlock)
                {
                    ((IMyTimerBlock)block).Trigger();
                }
                else if (block is IMySoundBlock)
                {
                    ((IMySoundBlock)block).Play();
                }
            }
        }
    }
}
