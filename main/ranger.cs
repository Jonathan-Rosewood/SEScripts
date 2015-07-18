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
        }
        else
        {
            Echo("Parallel lines???");
        }
    }
}
