private Rangefinder.LineSample first, second;

public void Main(string argument)
{
    var referenceGroup = ZALibrary.GetBlockGroupWithName(this, "Reference");
    var forwardGroup = ZALibrary.GetBlockGroupWithName(this, "Forward");
    if (referenceGroup == null ||
        forwardGroup == null)
    {
        throw new Exception("Missing groups!");
    }

    var reference = referenceGroup.Blocks[0];
    var forward = forwardGroup.Blocks[0];

    argument = argument.Trim().ToString();
    if (argument == "first" || argument.Length == 0)
    {
        first = new Rangefinder.LineSample(reference, forward);
    }
    else if (argument == "second")
    {
        second = new Rangefinder.LineSample(reference, forward);

        VRageMath.Vector3D target;
        if (Sighting.Compute(first, second, out target))
        {
            Echo("Target: " + target);
        }
        else
        {
            Echo("Parallel lines???");
        }
    }
}
