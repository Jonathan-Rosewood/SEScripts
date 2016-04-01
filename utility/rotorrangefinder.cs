//@ commons eventdriver rotorstepper rangefinder
public class RotorRangefinder
{
    private readonly RotorStepper rotorStepper = new RotorStepper(ROTOR_REFERENCE_GROUP);

    public void Init(ZACommons commons, EventDriver eventDriver)
    {
        rotorStepper.Init(commons, eventDriver);
    }

    public void HandleCommand(ZACommons commons, EventDriver eventDriver,
                              string argument, Action<ZACommons, Vector3D> targetAction)
    {
        argument = argument.Trim().ToLower();
        switch (argument)
        {
            case "compute":
                var firstReference = GetReference(commons, STATIC_REFERENCE_GROUP);
                var rotorReference = GetReference(commons, ROTOR_REFERENCE_GROUP);

                var first = new Rangefinder.LineSample(firstReference);
                var second = new Rangefinder.LineSample(rotorReference);
                Vector3D closestFirst, closestSecond;
                if (Rangefinder.Compute(first, second, out closestFirst, out closestSecond))
                {
                    // Take midpoint of closestFirst-closestSecond segment
                    var target = (closestFirst + closestSecond) / 2.0;
                    targetAction(commons, target);
                }
                break;
            default:
                rotorStepper.HandleCommand(commons, eventDriver, argument);
                break;
        }
    }

    private IMyCubeBlock GetReference(ZACommons commons, string groupName)
    {
        var group = commons.GetBlockGroupWithName(groupName);
        if (group == null)
        {
            throw new Exception("Missing group: " + groupName);
        }
        var controllers = ZACommons.GetBlocksOfType<IMyShipController>(group.Blocks);
        if (controllers.Count == 0)
        {
            throw new Exception("Expecting at least 1 ship controller in " + groupName);
        }
        return controllers[0];
    }
}
