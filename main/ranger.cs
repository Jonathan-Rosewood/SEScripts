private Rangefinder.LineSample first, second;

public void Main(string argument)
{
    var referenceGroup = ZALibrary.GetBlockGroupWithName(this, RANGEFINDER_REFERENCE_GROUP);
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

        VRageMath.Vector3D target;
        if (Rangefinder.Compute(first, second, out target))
        {
            Echo("Target: " + target);

            var targetGroup = ZALibrary.GetBlockGroupWithName(this, RANGEFINDER_TARGET_GROUP);
            if (targetGroup != null)
            {
                var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                                 target.GetDim(0),
                                                 target.GetDim(1),
                                                 target.GetDim(2));

                for (var e = ZALibrary.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).GetEnumerator(); e.MoveNext();)
                {
                    e.Current.WritePublicText(targetString);
                }
            }
        }
        else
        {
            Echo("Parallel lines???");
        }
    }
}
