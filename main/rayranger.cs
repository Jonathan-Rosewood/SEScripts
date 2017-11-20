//! Ray Ranger
//@ commons
public void TargetAction(ZACommons commons, Vector3D target, Vector3D velocity)
{
    // Output to terminal
    commons.Echo(string.Format("Target: {0:F2}, {1:F2}, {2:F2}",
                               target.GetDim(0),
                               target.GetDim(1),
                               target.GetDim(2)));
    var distance = (target - commons.Me.GetPosition()).Length();
    commons.Echo(string.Format("Distance: {0:F2} m", distance));

    // Also to text panel(s)
    var targetGroup = commons.GetBlockGroupWithName(RANGEFINDER_TARGET_GROUP);
    if (targetGroup != null)
    {
        var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                         target.GetDim(0),
                                         target.GetDim(1),
                                         target.GetDim(2),
                                         velocity.GetDim(0),
                                         velocity.GetDim(1),
                                         velocity.GetDim(2),
                                         distance);

        foreach (var panel in ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks))
        {
            panel.WritePublicText(targetString);
        }
    }
}

public void Main(string argument, UpdateType updateType)
{
    var commons = new ZACommons(this, updateType);

    var referenceGroup = commons.GetBlockGroupWithName(RANGEFINDER_REFERENCE_GROUP);
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: " + RANGEFINDER_REFERENCE_GROUP);
    }
    else if (referenceGroup.Blocks.Count != 1)
    {
        throw new Exception("Expecting exactly 1 block in group " + RANGEFINDER_REFERENCE_GROUP);
    }
    var camera = referenceGroup.Blocks[0] as IMyCameraBlock;
    if (camera == null)
    {
        throw new Exception("Expecting camera in group " + RANGEFINDER_REFERENCE_GROUP);
    }

    argument = argument.Trim().ToString();
    if (argument == "arm")
    {
        camera.EnableRaycast = true;
    }
    else if (argument == "disarm")
    {
        camera.EnableRaycast = false;
    }
    else if (argument == "snapshot")
    {
        var info = camera.Raycast(5000.0);
        if (!info.IsEmpty())
        {
            var position = info.HitPosition != null ? (Vector3D)info.HitPosition : info.Position;
            TargetAction(commons, position, new Vector3D(info.Velocity));
        }
    }
}
