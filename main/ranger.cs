//! Ranger
//@ commons rangefinder
public void TargetAction(ZACommons commons, Vector3D target)
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
                                         distance);

        foreach (var panel in ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks))
        {
            panel.WritePublicText(targetString);
        }
    }
}

private Rangefinder.LineSample first, second;

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
    var reference = referenceGroup.Blocks[0];

    argument = argument.Trim().ToString();
    if (argument == "first" || argument.Length == 0)
    {
        first = new Rangefinder.LineSample(reference);
    }
    else if (argument == "second")
    {
        second = new Rangefinder.LineSample(reference);

        Vector3D closestFirst, closestSecond;
        if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
        {
            // We're interested in the midpoint of the closestFirst-closestSecond segment
            var target = (closestFirst + closestSecond) / 2.0;
            TargetAction(commons, target);
        }
        else
        {
            Echo("Parallel lines???");
        }
    }
}
