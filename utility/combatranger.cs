//@ commons rangefinder
public class CombatRanger
{
    private Rangefinder.LineSample Origin;
    private Vector3D? Last = null;
    private StringBuilder Result = new StringBuilder();

    public void HandleCommand(ZACommons commons, string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(new char[] { ' ' }, 2);
        if (parts.Length < 2 || parts[0] != "rfinder") return;
        var command = parts[1];

        switch(command)
        {
            case "origin":
                {
                    var reference = GetReference(commons);
                    if (reference == null) return;
                    Origin = new Rangefinder.LineSample(reference);
                    Last = null;
                    Result.Clear();
                    break;
                }
            case "snapshot":
                {
                    var reference = GetReference(commons);
                    if (reference == null) return;
                    var second = new Rangefinder.LineSample(reference);

                    Last = null;
                    Result.Clear();

                    Vector3D closestFirst, closestSecond;
                    if (Rangefinder.Compute(Origin, second, out closestFirst,
                                            out closestSecond))
                    {
                        var target = (closestFirst + closestSecond) / 2.0;
                        Last = target;

                        Result.Append(string.Format("Target: {0:F2}, {1:F2}, {2:F2}",
                                                    target.GetDim(0),
                                                    target.GetDim(1),
                                                    target.GetDim(2)));
                        Result.Append('\n');

                        var targetVector = target - reference.GetPosition();
                        Result.Append(string.Format("Distance: {0} m", (ulong)(targetVector.Length() + 0.5)));

                        TargetAction(commons, target);
                    }
                    else
                    {
                        Result.Append("Parallel lines???");
                    }
                    break;
                }
            case "restore":
                if (Last != null)
                {
                    TargetAction(commons, (Vector3D)Last);
                }
                break;
        }
    }

    public void Display(ZACommons commons)
    {
        commons.Echo(Result.ToString());
    }

    private void TargetAction(ZACommons commons, Vector3D target)
    {
        var targetGroup = commons.GetBlockGroupWithName(RANGEFINDER_TARGET_GROUP);
        if (targetGroup != null)
        {
            var targetString = string.Format(RANGEFINDER_TARGET_FORMAT,
                                             target.GetDim(0),
                                             target.GetDim(1),
                                             target.GetDim(2));

            ZACommons.GetBlocksOfType<IMyTextPanel>(targetGroup.Blocks).ForEach(panel => {
                    panel.WritePublicText(targetString);
                });
        }
    }

    private IMyCubeBlock GetReference(ZACommons commons)
    {
        var referenceGroup = commons.GetBlockGroupWithName(RANGEFINDER_REFERENCE_GROUP);
        if (referenceGroup == null)
        {
            Result.Clear();
            Result.Append("Missing group: " + RANGEFINDER_REFERENCE_GROUP);
            return null;
        }
        var references = ZACommons.GetBlocksOfType<IMyTerminalBlock>(referenceGroup.Blocks,
                                                                     block => block.CubeGrid == commons.Me.CubeGrid);
        if (references.Count == 0)
        {
            Result.Clear();
            Result.Append("Expecting at least 1 block on the same grid: " + RANGEFINDER_REFERENCE_GROUP);
            return null;
        }
        return references[0];
    }
}
