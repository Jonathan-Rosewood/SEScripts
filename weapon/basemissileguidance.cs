//@ commons eventdriver
public abstract class BaseMissileGuidance
{
    protected Vector3D Target, TargetVelocity;
    protected TimeSpan LastTargetUpdate;

    // A remote update (antenna, another PB, etc.)
    public void HandleCommand(ZACommons commons, EventDriver eventDriver, string argument)
    {
        argument = argument.Trim().ToLower();
        var parts = argument.Split(';');
        if (parts.Length != 7) return;
        if (parts[0] != "tupdate") return;
        Target = new Vector3D();
        for (int i = 1; i < 4; i++)
        {
            Target.SetDim(i-1, double.Parse(parts[i]));
        }
        TargetVelocity = new Vector3D();
        for (int i = 4; i < 7; i++)
        {
            TargetVelocity.SetDim(i-4, double.Parse(parts[i]));
        }
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }

    // An update from e.g. an onboard seeker
    public void UpdateTarget(EventDriver eventDriver, Vector3D target, Vector3D velocity)
    {
        Target = target;
        TargetVelocity = velocity;
        LastTargetUpdate = eventDriver.TimeSinceStart;
    }
}
