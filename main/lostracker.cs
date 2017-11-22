//! LOS Tracker
//@ commons eventdriver
private readonly EventDriver eventDriver = new EventDriver();
private readonly LOSTracker losTracker = new LOSTracker();

private bool FirstRun = true;

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

        losTracker.Init(commons, eventDriver);
    }

    eventDriver.Tick(commons, argAction: () => {
            losTracker.HandleCommand(commons, eventDriver, argument);
        });
}

public class LOSTracker
{
    private IMyTerminalBlock LauncherReference;

    private Vector3D LauncherReferencePoint;
    private Vector3D LauncherReferenceDirection;

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        // Should we be holding on to this...?
        LauncherReference = SetLauncherReference(commons, TRACKER_REFERENCE_GROUP);

        eventDriver.Schedule(0, Run);
    }

    public void Run(ZACommons commons, EventDriver eventDriver)
    {
        SetLauncherReference(LauncherReference);

        var msg = string.Format("bupdate;{0};{1};{2};{3};{4};{5}",
                                LauncherReferencePoint.X,
                                LauncherReferencePoint.Y,
                                LauncherReferencePoint.Z,
                                LauncherReferenceDirection.X,
                                LauncherReferenceDirection.Y,
                                LauncherReferenceDirection.Z);
        BroadcastMessage(commons, msg);

        eventDriver.Schedule(TRACKER_UPDATE_RATE, Run);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        if (parts.Length != 2) return;
        if (parts[0] != "disconnect") return;

        // And reconstruct it.. heh.
        var msg = string.Format("disconnect;{0}", parts[1]);
        BroadcastMessage(commons, msg);
    }

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

    private void BroadcastMessage(ZACommons commons, string message)
    {
        var updateGroup = commons.GetBlockGroupWithName(TARGET_UPDATE_GROUP);
        if (updateGroup != null)
        {
            var broadcasted = false;
            foreach (var block in updateGroup.Blocks)
            {
                if (block is IMyLaserAntenna)
                {
                    ((IMyLaserAntenna)block).TransmitMessage(message);
                }
                else if (!broadcasted && block is IMyRadioAntenna)
                {
                    // Only if functional and enabled
                    var antenna = (IMyRadioAntenna)block;
                    if (antenna.IsFunctional && antenna.Enabled)
                    {
                        antenna.TransmitMessage(message, TRACKER_ANTENNA_TARGET);
                        broadcasted = true;
                    }
                }
            }
        }
    }
}
