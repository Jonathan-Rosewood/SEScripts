public void TargetAction(ZACommons commons, Vector3D target, double radius)
{
    var targetGroup = commons.GetBlockGroupWithName("GS Target");
    if (targetGroup != null)
    {
        var targetString = string.Format("{0};{1};{2};{3}",
                                         target.GetDim(0),
                                         target.GetDim(1),
                                         target.GetDim(2),
                                         radius);

        for (var e = ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
        {
            ((IMyTextPanel)e.Current).WritePublicText(targetString);
        }
    }

    // Also output to terminal
    commons.Echo(string.Format("Center: {0:F2}, {1:F2}, {2:F2}",
                               target.GetDim(0),
                               target.GetDim(1),
                               target.GetDim(2)));
    commons.Echo(string.Format("Radius: {0:F2} m", radius));
}

private Rangefinder.LineSample first, second;

public void Main(string argument)
{
    var commons = new ZACommons(this);

    var references = ZACommons.GetBlocksOfType<IMyRemoteControl>(commons.Blocks);
    if (references.Count < 1)
    {
        throw new Exception("Expecting at least 1 remote control");
    }
    var reference = (IMyRemoteControl)references[0];

    var gravity = reference.GetNaturalGravity();
    if (gravity.Length() == 0.0)
    {
        throw new Exception("Expecting natural gravity");
    }

    argument = argument.Trim().ToString();
    if (argument == "first" || argument.Length == 0)
    {
        first = new Rangefinder.LineSample(reference, gravity);
    }
    else if (argument == "second")
    {
        second = new Rangefinder.LineSample(reference, gravity);

        Vector3D closestFirst, closestSecond;
        if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
        {
            var center = (closestFirst + closestSecond) / 2.0;
            var radius = (reference.GetPosition() - center).Length();
            TargetAction(commons, center, radius);
        }
        else
        {
            Echo("Parallel lines???");
        }
    }
}
