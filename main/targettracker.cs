//! Target Tracker
//@ commons eventdriver
private readonly EventDriver eventDriver = new EventDriver();
private readonly TargetTracker targetTracker = new TargetTracker();

private bool FirstRun = true;

public void MyTargetAction(ZACommons commons, Vector3D target, Vector3D velocity)
{
    // Compose message
    var targetString = string.Format("tupdate;{0};{1};{2};{3};{4};{5}",
                                     target.X, target.Y, target.Z,
                                     velocity.X, velocity.Y, velocity.Z);

    var updateGroup = commons.GetBlockGroupWithName(TARGET_UPDATE_GROUP);
    if (updateGroup != null)
    {
        var broadcasted = false;
        foreach (var block in updateGroup.Blocks)
        {
            if (block is IMyProgrammableBlock)
            {
                ((IMyProgrammableBlock)block).TryRun(targetString);
            }
            else if (block is IMyLaserAntenna)
            {
                ((IMyLaserAntenna)block).TransmitMessage(targetString);
            }
            else if (!broadcasted && block is IMyRadioAntenna)
            {
                // Only if functional and enabled
                var antenna = (IMyRadioAntenna)block;
                if (antenna.IsFunctional && antenna.Enabled)
                {
                    antenna.TransmitMessage(targetString, TRACKER_ANTENNA_TARGET);
                    broadcasted = true;
                }
            }
        }
    }
}

Program()
{
    // Kick things once, FirstRun will take care of the rest
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType)
{
    var commons = new ZACommons(this, updateType);

    if (FirstRun)
    {
        FirstRun = false;

        targetTracker.Init(commons, eventDriver, MyTargetAction);
    }

    eventDriver.Tick(commons, preAction: () => {
            targetTracker.HandleCommand(commons, eventDriver, argument);
        },
        postAction: () => {
            targetTracker.Display(commons, eventDriver);
        });
}

public class TargetTracker
{
    private Action<ZACommons, Vector3D, Vector3D> TargetAction;

    private const int IDLE = 0;
    private const int ARMED = 1;
    private const int INITIAL = 2;
    private const int LOCKED = 3;

    private int Mode = IDLE;

    private double RaycastRange;
    private TimeSpan? LastUpdate;

    // Target data
    private long TargetID;
    private Vector3D TargetOffset;

    public void Init(ZACommons commons, EventDriver eventDriver,
                     Action<ZACommons, Vector3D, Vector3D> targetAction)
    {
        TargetAction = targetAction;

        // Get things into a known state
        var camera = GetMainCamera(commons);
        camera.EnableRaycast = false;
        Mode = IDLE;
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
                }
                break;
            case "disarm":
                {
                    var camera = GetMainCamera(commons);
                    camera.EnableRaycast = false;
                    Mode = IDLE;
                    break;
                }
            case "lock":
                {
                    if (Mode == IDLE)
                    {
                        // Enable raycast for user
                        var camera = GetMainCamera(commons);
                        camera.EnableRaycast = true;
                        Mode = ARMED;
                    }
                    BeginLock(commons, eventDriver);
                    break;
                }
            case "unlock":
                if (Mode != IDLE)
                {
                    Mode = ARMED;
                }
                break;
        }
    }

    public void BeginLock(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != ARMED && Mode != LOCKED) return;

        RaycastRange = INITIAL_RAYCAST_RANGE;
        LastUpdate = null;
        if (Mode == ARMED) eventDriver.Schedule(0, Lock);
        Mode = INITIAL;
    }

    public void Lock(ZACommons commons, EventDriver eventDriver)
    {
        if (Mode != INITIAL && Mode != LOCKED) return;

        var camera = GetMainCamera(commons);

        // Can we raycast the desired distance?
        var scanTime = camera.TimeUntilScan(RaycastRange);
        if (scanTime > 0)
        {
            // Try later
            eventDriver.Schedule((double)scanTime / 1000.0, Lock);
            return;
        }

        var info = camera.Raycast(RaycastRange);
        if (info.IsEmpty())
        {
            // Missed? Try again ASAP
            eventDriver.Schedule(1, Lock);
            return;
        }

        Vector3D position;
        if (Mode == INITIAL)
        {
            position = (Vector3D)info.HitPosition;

            // Initial raycast, capture TargetID and TargetOffset
            TargetID = info.EntityId;
            var offset = position - info.Position;
            var toLocal = MatrixD.Invert(info.Orientation);
            TargetOffset = Vector3D.Transform(offset, toLocal);

            Mode = LOCKED;
        }
        else
        {
            if (info.EntityId != TargetID)
            {
                // Hit a different target, try again ASAP
                eventDriver.Schedule(1, Lock);
                return;
            }

            // Use original offset with new orientation and position
            position = info.Position + Vector3D.Transform(TargetOffset, info.Orientation);

            // Since info.Position is actually based on the target's bounding box,
            // we might actually be hosed if the target gets significantly damaged...
        }

        // Update next raycast distance (with buffer)
        RaycastRange = (info.Position - camera.GetPosition()).Length() * RAYCAST_RANGE_BUFFER;
        LastUpdate = eventDriver.TimeSinceStart;
        // And call TargetAction
        TargetAction(commons, position, new Vector3D(info.Velocity));

        eventDriver.Schedule(TRACKER_UPDATE_RATE, Lock);
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
            case INITIAL:
                commons.Echo("Tracker: Searching");
                commons.Echo(string.Format("Max. Range: {0:F2} m", RaycastRange));
                break;
            case LOCKED:
                commons.Echo("Tracker: Locked");
                commons.Echo(string.Format("Max. Range: {0:F2} m", RaycastRange));
                commons.Echo(string.Format("Target ID: {0:X}", TargetID));
                if (LastUpdate != null) commons.Echo(string.Format("Last Update: {0:F1} s", (eventDriver.TimeSinceStart - (TimeSpan)LastUpdate).TotalSeconds));
                break;
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
}
