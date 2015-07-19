private Rangefinder.LineSample first, second;

public void Main(string argument)
{
    var referenceGroup = ZALibrary.GetBlockGroupWithName(this, "Reference");
    if (referenceGroup == null)
    {
        throw new Exception("Missing group: Reference");
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

            var targetGroup = ZALibrary.GetBlockGroupWithName(this, "CM Target");
            if (targetGroup != null)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(target.GetDim(0));
                builder.Append(';');
                builder.Append(target.GetDim(1));
                builder.Append(';');
                builder.Append(target.GetDim(2));
                var targetString = builder.ToString();

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
